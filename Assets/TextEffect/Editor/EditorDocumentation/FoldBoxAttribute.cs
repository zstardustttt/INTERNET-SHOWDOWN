using UnityEngine;

namespace EasyTextEffects.Editor.EditorDocumentation
{
    public class FoldBoxAttribute : PropertyAttribute
    {
        public enum ContentType
        {
            Text, Image
        }
        public string Title { get; }
        public string[] Content { get; }
        public ContentType[] ContentTypes { get; }
        public bool Inline { get; }

        public FoldBoxAttribute(string _title, string[] _content, ContentType[] _contentTypes, bool _inline = true)
        {
            Title = _title;
            Content = _content;
            ContentTypes = _contentTypes;
            Inline = _inline;
        }
    }
}