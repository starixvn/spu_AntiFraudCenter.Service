namespace Fanex.NAP.SPU.AFC.Shares.ValidationData
{
    using System.ComponentModel.DataAnnotations;

    public class RequiredAtLeastAttribute : ValidationAttribute
    {
        private readonly RequiredAttribute _innerAttribute = new RequiredAttribute();

        public string _dependentProperty1 { get; set; }

        public string _dependentProperty2 { get; set; }

        public string _dependentProperty3 { get; set; }

        public RequiredAtLeastAttribute(string dependentProperty1, string dependentProperty2, string dependentProperty3)
        {
            this._dependentProperty1 = dependentProperty1;
            this._dependentProperty2 = dependentProperty2;
            this._dependentProperty3 = dependentProperty3;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var field1 = validationContext.ObjectType.GetProperty(_dependentProperty1);
            var field2 = validationContext.ObjectType.GetProperty(_dependentProperty2);
            var field3 = validationContext.ObjectType.GetProperty(_dependentProperty3);

            if (field1 != null && field2 != null && field3 != null)
            {
                var dependentValue1 = field1.GetValue(validationContext.ObjectInstance, null);
                var dependentValue2 = field2.GetValue(validationContext.ObjectInstance, null);
                var dependentValue3 = field3.GetValue(validationContext.ObjectInstance, null);

                if ((dependentValue1 != null && _innerAttribute.IsValid(dependentValue1))
                    || (dependentValue2 != null && _innerAttribute.IsValid(dependentValue2))
                    || (dependentValue3 != null && _innerAttribute.IsValid(dependentValue3))
                    || _innerAttribute.IsValid(value))
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult($"Required at least 1 of 4 information (Phone, Email, DocumentID, BankAccountID)");
            }
            else
            {
                return new ValidationResult(FormatErrorMessage($"{ _dependentProperty1 }, {_dependentProperty2}, {_dependentProperty3}"));
            }
        }
    }
}