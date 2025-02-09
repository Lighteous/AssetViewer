﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;

namespace RDA.Data {

  public class Description : IEquatable<Description> {

    #region Public Properties

    public string ID { get; set; }

    public Dictionary<Languages, string> Languages { get; set; } = new Dictionary<Languages, string>();

    public Icon Icon { get; set; }
    public DescriptionFontStyle FontStyle { get; set; }
    public Description AdditionalInformation { get; set; }

    #endregion Public Properties

    #region Public Constructors
    public Description(XElement asset, DescriptionFontStyle fontStyle = default) {
      var tempAsset = asset.DescendantsAndSelf().FirstOrDefault(i => i.Name.LocalName == "Asset");
      ID = tempAsset.XPathSelectElement("Values/Text/TextOverride")?.Value ?? tempAsset.XPathSelectElement("Values/Standard/GUID").Value;
      SetDescription(ID, fontStyle, tempAsset.XPathSelectElement("Values/Standard/GUID").Value, (GameTypes)Enum.Parse(typeof(GameTypes), tempAsset.Attribute("GameType").Value));
    }

    public Description(string id, GameTypes gameType, DescriptionFontStyle fontStyle = default) {
      ID = id;
      if (Assets.GUIDs.TryGetValue(id, out XElement asset, gameType)) {
        ID = asset.XPathSelectElement("Values/Text/TextOverride")?.Value ?? asset.XPathSelectElement("Values/Standard/GUID").Value;
      }
      SetDescription(ID, fontStyle, id, gameType);
    }

    private void SetDescription(string id, DescriptionFontStyle fontStyle, string iconId, GameTypes gameType) {
      ID = id;
      if (Assets.Descriptions.TryGetValue(id, out var languages)) {
        foreach (var item in languages) {
          Languages.Add(item.Key, item.Value);
        }
      }
      else if (Assets.CustomDescriptions.TryGetValue(id, out languages)) {
        foreach (var item in languages) {
          Languages.Add(item.Key, item.Value);
        }
      }
      else {
        Languages.Add(Data.Languages.English, id);
      }
      if (Assets.Icons.TryGetValue(iconId, out var icon, gameType)) {
        Icon = new Icon(icon);
      }
      FontStyle = fontStyle;
    }

    #endregion Public Constructors

    #region Public Methods

    public Description InsertBefore(Description description) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{(description.Languages.TryGetValue(item.Key, out var value) ? value : description.Languages.First().Value)} {item.Value}";
      }
      SetNewId();
      return this;
    }

    public Description InsertBefore(string value) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{value} {item.Value}";
      }
      SetNewId();
      return this;
    }

    public Description Append(string value) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{item.Value}{value}";
      }
      SetNewId();
      return this;
    }

    public Description Append(Description description) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{item.Value}{(description.Languages.TryGetValue(item.Key, out var value) ? value : description.Languages.First().Value)}";
      }
      SetNewId();
      return this;
    }

    public Description AppendWithSpace(string value) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{item.Value} {value}";
      }
      SetNewId();
      return this;
    }

    public Description AppendWithSpace(Description description) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{item.Value} {(description.Languages.TryGetValue(item.Key, out var value) ? value : description.Languages.First().Value)}";
      }
      SetNewId();
      return this;
    }

    public Description AppendInBraces(Description description) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{item.Value} ({(description.Languages.TryGetValue(item.Key, out var value) ? value : description.Languages.First().Value)})";
      }
      SetNewId();
      return this;
    }

    public Description AppendInBraces(string description) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = $"{item.Value} ({description})";
      }
      SetNewId();
      return this;
    }

    public Description Remove(string value) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = item.Value.Replace(HttpUtility.HtmlDecode(value), "");
      }
      SetNewId();
      return this;
    }

    public Description Replace(string oldValue, Description newValue) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = item.Value.Replace(HttpUtility.HtmlDecode(oldValue), newValue.Languages.TryGetValue(item.Key, out var value) ? value : newValue.Languages.First().Value);
      }
      SetNewId();
      return this;
    }

    public Description Replace(string oldValue, IEnumerable<Description> newValue, Func<IEnumerable<string>, string> format) {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = item.Value.Replace(HttpUtility.HtmlDecode(oldValue), format?.Invoke(newValue.Select(v => v.Languages.TryGetValue(item.Key, out var value) ? value : v.Languages.First().Value)));
      }
      SetNewId();
      return this;
    }

    public Description Trim() {
      foreach (var item in Languages.ToArray()) {
        Languages[item.Key] = item.Value.Trim();
      }
      SetNewId();
      return this;
    }

    public Description ChangeID(string Id) {
      ID = Id;
      return this;
    }

    public XElement ToXml(string name) {
      ID = GetOrCheckExistenz();
      var result = new XElement(name);
      result.Add(new XAttribute("ID", ID));
      if (Icon?.Filename is string icon) {
        result.Add(new XAttribute("I", icon));
      }
      if (FontStyle != default) {
        result.Add(new XAttribute("FS", (int)FontStyle));
      }
      if (AdditionalInformation != null) {
        result.Add(AdditionalInformation.ToXml("AI"));
      }

      return result;
    }

    public void SetNewId() {
      ID = Languages.First().Value.GetHashCode().ToString();
    }

    public override string ToString() {
      return Languages.First().Value;
    }

    public bool Equals(Description other) {
      return ID == other.ID && Languages.First().Value == other.Languages.First().Value && Icon.Filename == other.Icon.Filename;
    }

    #endregion Public Methods

    #region Public Fields

    public static readonly Dictionary<string, Description> GlobalDescriptions = new Dictionary<string, Description>();

    #endregion Public Fields

    #region Internal Methods

    internal static Description Join(IEnumerable<Description> regions, string seperator, GameTypes gameType) {
      if (regions?.Any() == true) {
        var desc = new Description(regions.First().ID, gameType);
        foreach (var item in regions.Skip(1)) {
          desc.Append(seperator).Append(item);
        }
        return desc;
      }
      return null;
    }

    #endregion Internal Methods

    #region Private Methods

    private string GetOrCheckExistenz() {
      if (GlobalDescriptions.TryGetValue(Languages.First().Value, out var value)) {
        return value.ID;
      }
      else {
        GlobalDescriptions.Add(Languages.First().Value, this);

        return ID;
      }
    }

    #endregion Private Methods
  }
}