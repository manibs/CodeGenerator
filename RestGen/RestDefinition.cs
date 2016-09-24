using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace RestGen
{
    [DebuggerDisplay("{Services.Count} services")]
    public sealed class RestDefinition
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ServiceDefinitions _services = new ServiceDefinitions();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ModelDefinitions _models = new ModelDefinitions();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ServiceDefinitions Services
        {
            [DebuggerStepThrough]
            get { return _services; }
        }

        public ModelDefinitions Models
        {
            [DebuggerStepThrough]
            get { return _models; }
        }
    }

    public sealed class ServiceDefinitions : Collection<ServiceDefinition>
    {
        public ServiceDefinition this[string serviceName]
        {
            get { return this.FirstOrDefault(sd => sd.Name.Equals(serviceName)); }
        }
    }

    [DebuggerDisplay("Service {Name} ({Operations.Count} operations)")]
    public sealed class ServiceDefinition
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _name;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly OperationDefinitions _operations = new OperationDefinitions();

        public ServiceDefinition(string name)
        {
            _name = name;
        }

        public string Name
        {
            [DebuggerStepThrough]
            get { return _name; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public OperationDefinitions Operations
        {
            [DebuggerStepThrough]
            get { return _operations; }
        }
    }

    public sealed class OperationDefinitions : Collection<OperationDefinition>
    {
    }

    [DebuggerDisplay("{Verb} {Path}")]
    public sealed class OperationDefinition
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _verb;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _path;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _name;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _description;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DataType _successResponseType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DataType _errorResponseType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ParameterDefinitions _parameters = new ParameterDefinitions();

        public OperationDefinition(string verb, string path, string name, string description,
            DataType successResponseType, DataType errorResponseType)
        {
            _verb = verb;
            _path = path;
            _name = name;
            _description = description;
            _successResponseType = successResponseType;
            _errorResponseType = errorResponseType;
        }

        public string Verb
        {
            [DebuggerStepThrough]
            get { return _verb; }
        }

        public string Path
        {
            [DebuggerStepThrough]
            get { return _path; }
        }

        public string Name
        {
            [DebuggerStepThrough]
            get { return _name; }
        }

        public string Description
        {
            [DebuggerStepThrough]
            get { return _description; }
        }

        public DataType SuccessResponseType
        {
            [DebuggerStepThrough]
            get { return _successResponseType; }
        }

        public DataType ErrorResponseType
        {
            [DebuggerStepThrough]
            get { return _errorResponseType; }
        }

        public ParameterDefinitions Parameters
        {
            [DebuggerStepThrough]
            get { return _parameters; }
        }
    }

    public sealed class ParameterDefinitions : Collection<ParameterDefinition>
    {
    }

    [DebuggerDisplay("[{Location}] {Name}: {Type} ({Requirement})")]
    public sealed class ParameterDefinition
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _name;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ParameterLocation _location;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Requirement _requirement;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DataType _type;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _default; //TODO: Change type to actual type or object

        public ParameterDefinition(string name, ParameterLocation location, Requirement requirement,
            DataType type, string @default)
        {
            _name = name;
            _location = location;
            _requirement = requirement;
            _type = type;
            _default = @default;
        }

        public string Name
        {
            [DebuggerStepThrough]
            get { return _name; }
        }

        public ParameterLocation Location
        {
            [DebuggerStepThrough]
            get { return _location; }
        }

        public Requirement Requirement
        {
            [DebuggerStepThrough]
            get { return _requirement; }
        }

        public DataType Type
        {
            [DebuggerStepThrough]
            get { return _type; }
        }

        public string Default
        {
            [DebuggerStepThrough]
            get { return _default; }
        }
    }

    public sealed class ModelDefinitions : Collection<ModelDefinition>
    {
        public bool Contains(string name)
        {
            return this.Any(md => md.Name.Equals(name));
        }
    }

    [DebuggerDisplay("Model {Name} ({Properties.Count} properties)")]
    public sealed class ModelDefinition
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _name;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ModelPropertyDefinitions _properties = new ModelPropertyDefinitions();

        public ModelDefinition(string name)
        {
            _name = name;
        }

        public string Name
        {
            [DebuggerStepThrough]
            get { return _name; }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ModelPropertyDefinitions Properties
        {
            [DebuggerStepThrough]
            get { return _properties; }
        }
    }

    public sealed class ModelPropertyDefinitions : Collection<ModelPropertyDefinition>
    {
    }

    [DebuggerDisplay("{Name}: {Type} ({Requirement})")]
    public sealed class ModelPropertyDefinition
    {
        public ModelPropertyDefinition(string name, DataType type, Requirement requirement)
        {
            Name = name;
            Type = type;
            Requirement = requirement;
        }

        public string Name { get; }

        public DataType Type { get; }

        public Requirement Requirement { get; }
    }

    public sealed class DataType
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Type _primitiveType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string _complexType;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly bool _isCollection;

        public DataType(Type primitiveType, bool isCollection)
        {
            _primitiveType = primitiveType;
            _isCollection = isCollection;
        }

        public DataType(string complexType, bool isCollection)
        {
            _complexType = complexType;
            _isCollection = isCollection;
        }

        public Type PrimitiveType
        {
            [DebuggerStepThrough]
            get { return _primitiveType; }
        }

        public string ComplexType
        {
            [DebuggerStepThrough]
            get { return _complexType; }
        }

        public bool IsCollection
        {
            [DebuggerStepThrough]
            get { return _isCollection; }
        }

        public bool IsPrimitive => _primitiveType != null;

        public bool IsComplex => _complexType != null;

        public override string ToString()
        {
            return _primitiveType != null ? _primitiveType.Name : _complexType;
        }
    }
}
