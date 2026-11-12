// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;

#nullable disable
using SR = MS.Internal.PresentationFramework.SR;

namespace System.Windows
{
    /// <summary>
    ///     TargetType property setting class.
    /// </summary>
    public class Setter : SetterBase, ISupportInitialize
    {
        /// <summary>
        ///     Property Setter construction - set everything to null or DependencyProperty.UnsetValue
        /// </summary>
        public Setter()
        {

        }

        /// <summary>
        ///     Property Setter construction - given property and value
        /// </summary>
        public Setter( DependencyProperty property, object value )
        {
            Initialize( property, value, null );
        }

        /// <summary>
        ///     Property Setter construction - given property, value, and string identifier for child node.
        /// </summary>
        public Setter( DependencyProperty property, object value, string targetName )
        {
            Initialize( property, value, targetName );
        }

        /// <summary>
        ///     Method that does all the initialization work for the constructors.
        /// </summary>
        private void Initialize( DependencyProperty property, object value, string target )
        {
            if( value == DependencyProperty.UnsetValue )
            {
                throw new ArgumentException(SR.SetterValueCannotBeUnset);
            }

            CheckValidProperty(property);

            // No null check for target since null is a valid value.

            _property = property;
            _value = value;
            _target = target;
        }

        private void CheckValidProperty( DependencyProperty property)
        {
            ArgumentNullException.ThrowIfNull(property);
            if (property.ReadOnly)
            {
                // Read-only properties will not be consulting Style/Template/Trigger Setter for value.
                //  Rather than silently do nothing, throw error.
                throw new ArgumentException(SR.Format(SR.ReadOnlyPropertyNotAllowed, property.Name, GetType().Name));
            }
            if( property == FrameworkElement.NameProperty)
            {
                throw new InvalidOperationException(SR.Format(SR.CannotHavePropertyInStyle, FrameworkElement.NameProperty.Name));
            }
        }

        /// <summary>
        ///     Seals this setter
        /// </summary>
        internal override void Seal()
        {

            // Do the validation that can't be done until we know all of the property
            // values.

            DependencyProperty dp = Property;
            object value = ValueInternal;

            if (dp == null)
            {
                throw new ArgumentException(SR.Format(SR.NullPropertyIllegal, "Setter.Property"));
            }

            if( String.IsNullOrEmpty(TargetName))
            {
                // Setter on container is not allowed to affect the StyleProperty.
                if (dp == FrameworkElement.StyleProperty)
                {
                    throw new ArgumentException(SR.StylePropertyInStyleNotAllowed);
                }
            }

            // Value needs to be valid for the DP, or a deferred reference, or one of the supported
            // markup extensions.

            if (!dp.IsValidValue(value))
            {
                // The only markup extensions supported by styles is resources and bindings.
                if (value is MarkupExtension)
                {
                    if ( !(value is DynamicResourceExtension) && !(value is System.Windows.Data.BindingBase) )
                    {
                        throw new ArgumentException(SR.Format(SR.SetterValueOfMarkupExtensionNotSupported,
                                                           value.GetType().Name));
                    }
                }

                else if (!(value is DeferredReference))
                {
                    throw new ArgumentException(SR.Format(SR.InvalidSetterValue, value, dp.OwnerType, dp.Name));
                }
            }

            // Freeze the value for the setter
            StyleHelper.SealIfSealable(_value);

            base.Seal();
        }

        /// <summary>
        ///    Property that is being set by this setter
        /// </summary>
        [Ambient]
        [DefaultValue(null)]
        [Localizability(LocalizationCategory.None, Modifiability = Modifiability.Unmodifiable, Readability = Readability.Unreadable)] // Not localizable by-default
        public DependencyProperty Property
        {
            get { return _property; }
            set
            {
                CheckValidProperty(value);
                CheckSealed();
                _property = value;
            }

        }

        /// <summary>
        ///    Property value that is being set by this setter
        /// </summary>
        [System.Windows.Markup.DependsOn("Property")]
        [System.Windows.Markup.DependsOn("TargetName")]
        [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)] // Not localizable by-default
        public object Value
        {
            get
            {
                // Inflate the deferred reference if the _value is one of those.
                DeferredReference deferredReference = _value as DeferredReference;
                if (deferredReference != null)
                {
                    _value = deferredReference.GetValue(BaseValueSourceInternal.Unknown);
                }

                return _value;
            }

            set
            {
                if( value == DependencyProperty.UnsetValue )
                {
                    throw new ArgumentException(SR.SetterValueCannotBeUnset);
                }

                CheckSealed();

                // No Expression support
                if( value is Expression )
                {
                    throw new ArgumentException(SR.StyleValueOfExpressionNotSupported);
                }


                _value = value;
            }
        }

        /// <summary>
        ///     Internal property used so that we obtain the value as
        ///     is without having to inflate the DeferredReference.
        /// </summary>
        internal object ValueInternal
        {
            get { return _value; }
        }

        /// <summary>
        ///     When the set is directed at a child node, this string
        /// identifies the intended target child node.
        /// </summary>
        [DefaultValue(null)]
        [Ambient]
        public string TargetName
        {
            get
            {
                return _target;
            }
            set
            {
                // Setting to null is allowed, to clear out value.
                CheckSealed();
                _target = value;
            }
        }

        #region ISupportInitialize Members

        void ISupportInitialize.BeginInit()
        {
        }

        void ISupportInitialize.EndInit()
        {
        }

        #endregion

        private DependencyProperty    _property = null;
        private object                _value    = DependencyProperty.UnsetValue;
        private string                _target   = null;

    }

}

