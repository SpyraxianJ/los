using UnityEngine;
using UnityEngine.UI;

public class ToggleController : MonoBehaviour
{
    public Toggle toggle;
    public Color selectedBackgroundColor;

    private ColorBlock originalColors;

    private void Start()
    {
        // Save the original colors
        originalColors = toggle.colors;

        // Attach the listener for the OnValueChanged event
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            // Change the background color when the Toggle is selected
            ColorBlock newColors = toggle.colors;
            newColors.normalColor = selectedBackgroundColor;
            newColors.selectedColor = selectedBackgroundColor;
            newColors.highlightedColor = selectedBackgroundColor;
            toggle.colors = newColors;
        }
        else
        {
            // Restore the original colors when the Toggle is not selected
            toggle.colors = originalColors;
        }
    }
}
