namespace Fanex.NAP.SPU.AFC.Shares.ValidationData
{
    using System.ComponentModel.DataAnnotations;

    public class RequiredDependencyAttribute : ValidationAttribute
    {
        private readonly RequiredAttribute _innerAttribute = new RequiredAttribute();

        public string _dependentProperty { get; set; }

        public object _targetValue { get; set; }

        public RequiredDependencyAttribute(string dependentProperty)
        {
            this._dependentProperty = dependentProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var field = validationContext.ObjectType.GetProperty(_dependentProperty);

            if (field != null)
            {
                var dependentValue = field.GetValue(validationContext.ObjectInstance, null);

                if (dependentValue != null
                    && _innerAttribute.IsValid(dependentValue)
                    && !_innerAttribute.IsValid(value))
                {
                    string name = validationContext.DisplayName;
                    string dependentName = field.Name;

                    return new ValidationResult($"{name} is required, because {dependentName} has value.");
                }

                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult(FormatErrorMessage(_dependentProperty));
            }
        }
    }
}