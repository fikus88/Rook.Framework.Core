﻿namespace Rook.Framework.Core.Monitoring
{
    public class BuildInfoLabel
    {
        public BuildInfoLabel(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }

        public override string ToString()
        {
            return $"{Name}={Value}";
        }
    }
}