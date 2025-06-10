using System;
using System.Collections.Generic;

namespace src.Editor.CMSEditor.Templates
{
    [Serializable]
    public class EntityTemplate
    {
        public string templateName;
        public List<SerializableComponent> components;
    }
}