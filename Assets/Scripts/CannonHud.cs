using UnityEngine;
using TMPro;

public sealed class CannonHud : MonoBehaviour
{
    public CannonController cannon;
    public TMP_Text readout;
    void Update()
    {
        readout.text = $"NEXT ROUND  /  СЛЕДУЮЩЕЕ ЯДРО\n{cannon.NextMass:F2} kg     {cannon.NextRadius:F3} m\n\nSHOTS / ВЫСТРЕЛЫ   {cannon.Shots}     HITS / ПОПАДАНИЯ   {cannon.Hits}";
    }
}
