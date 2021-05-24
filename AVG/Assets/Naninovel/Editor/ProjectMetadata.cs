// Copyright 2017-2021 Elringus (Artyom Sovetnikov). All rights reserved.

using System;
using System.Collections.Generic;

namespace Naninovel
{
    [Serializable]
    public class ProjectMetadata
    {
        public List<CommandMetadata> commands;
        public List<ProjectResource> resources;
        public List<ProjectActor> actors;
        public List<string> predefinedVariables;
        public List<string> expressionFunctions;
        
        [Serializable]
        public class ProjectResource
        {
            public string name;
            public string pathPrefix;
        }
    
        [Serializable]
        public class ProjectActor
        {
            public string id;
            public string pathPrefix;
            public string displayName;
            public List<string> appearances;
        }
    
        [Serializable]
        public class CommandMetadata
        {
            public string id;
            public string alias;
            public bool localizable;
            public string summary;
            public string remarks;
            public string examples;
            public List<ParameterMetadata> @params;
        }
    
        [Serializable]
        public class ParameterMetadata
        {
            public string id;
            public string alias;
            public bool nameless;
            public bool required;
            public bool localizable;
            public DataType dataType;
            public string defaultValue;
            public string summary;
            public string resourcePathPrefix;
            public int resourcePathPrefixNamedId;
            public string actorPathPrefix;
            public int actorPathPrefixNamedId;
            public bool appearance;
            public int appearanceNamedId;
            public string constant;
            public int constantNamedId;
        }
    
        [Serializable]
        public class DataType
        {
            public string kind;
            public string contentType;
        }
    }
}
