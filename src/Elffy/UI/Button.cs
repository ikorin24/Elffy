#nullable enable

namespace Elffy.UI
{
    /// <summary>Button class which fires event on mouse click.</summary>
    public class Button : Executable
    {
        private bool _isEnabled;
        private bool _isEnabledChanged;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if(_isEnabled == value) { return; }
                _isEnabled = value;
                _isEnabledChanged = true;
            }
        }

        /// <summary>Create new <see cref="Button"/></summary>
        public Button()
        {
            _isEnabled = true;
        }

        protected override void OnUIEvent()
        {
            var isEnabledChanged = _isEnabledChanged;
            _isEnabledChanged = false;
            if(isEnabledChanged && _isEnabled == false) {
                ForceCancelExecutableEventFlow();
            }
            base.OnUIEvent();
        }
    }
}
