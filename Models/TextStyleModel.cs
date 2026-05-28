using System.Runtime.Serialization;
using System.Text.Json.Serialization;
[JsonConverter(typeof(JsonStringEnumConverter))]
[DataContract]
public enum TextStyleModel {
    NONE,
    BOLD,
    ITALIC,
    UNDERLINE,
    TEXT_ALIGNMENT,
    QUOTE,
    REFER_LINK,
    IMAGE,
    BULLET_LIST,
    TEXT_SIZE_LARGE
}