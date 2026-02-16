using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextUpdater : MonoBehaviour
{
    public CityStats cityStats;
    public TMP_Text PopulationText;
    public TMP_Text MoneyText;
    public TMP_Text PowerText;
    public TMP_Text IncomeText;
    void Update()
    {
        PopulationText.text = "Population: " + cityStats.population;
        MoneyText.text = "Money: $" + cityStats.money;
        PowerText.text = "Power: " + cityStats.power;
        IncomeText.text = "Income: $" + cityStats.income + "/turn";
    }
}
