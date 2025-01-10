using UnityEngine;
using System.Collections;
using TMPro;

public class FPSCountDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private string text = "FPS: ";

    private float count;

    private IEnumerator Start()
    {
        while (true)
        {
            count = 1f / Time.unscaledDeltaTime;
            textField.SetText(text + Mathf.Round(count));

            yield return new WaitForSeconds(0.5f);
        }
    }
}
