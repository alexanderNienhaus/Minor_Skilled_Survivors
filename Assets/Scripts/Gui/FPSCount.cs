using UnityEngine;
using System.Collections;
using TMPro;

public class FPSCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;

    private float count;

    private IEnumerator Start()
    {
        while (true)
        {
            count = 1f / Time.unscaledDeltaTime;
            textField.SetText("FPS: " + Mathf.Round(count));

            yield return new WaitForSeconds(0.5f);
        }
    }
}
