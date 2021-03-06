// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Expressions
{
    using CoreWf.Runtime;

    public sealed class VariableValue<T> : EnvironmentLocationValue<T>
    {
        public VariableValue()
            : base()
        {
        }

        public VariableValue(Variable variable)
            : base()
        {
            this.Variable = variable;
        }

        public Variable Variable
        {
            get;
            set;
        }

        public override LocationReference LocationReference
        {
            get { return this.Variable; }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (this.Variable == null)
            {
                metadata.AddValidationError(SR.VariableMustBeSet);
            }
            else
            {
                if (!(this.Variable is Variable<T>) && !TypeHelper.AreTypesCompatible(this.Variable.Type, typeof(T)))
                {
                    metadata.AddValidationError(SR.VariableTypeInvalid(this.Variable, typeof(T), this.Variable.Type));
                }

                if (!this.Variable.IsInTree)
                {
                    metadata.AddValidationError(SR.VariableShouldBeOpen(this.Variable.Name));
                }

                if (!metadata.Environment.IsVisible(this.Variable))
                {
                    metadata.AddValidationError(SR.VariableNotVisible(this.Variable.Name));
                }
            }
        }

        public override string ToString()
        {
            if (Variable != null && !string.IsNullOrEmpty(Variable.Name))
            {
                return Variable.Name;
            }

            return base.ToString();
        }
    }
}
