namespace BlazorBaseUI.Checkbox;

public readonly record struct CheckboxIndicatorState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool Indeterminate,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    TransitionStatus TransitionStatus)
{
    internal static CheckboxIndicatorState FromRootState(
        CheckboxRootState rootState,
        TransitionStatus transitionStatus) => new(
            Checked: rootState.Checked,
            Disabled: rootState.Disabled,
            ReadOnly: rootState.ReadOnly,
            Required: rootState.Required,
            Indeterminate: rootState.Indeterminate,
            Valid: rootState.Valid,
            Touched: rootState.Touched,
            Dirty: rootState.Dirty,
            Filled: rootState.Filled,
            Focused: rootState.Focused,
            TransitionStatus: transitionStatus);
}
