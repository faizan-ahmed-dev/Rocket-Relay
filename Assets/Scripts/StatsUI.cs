using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{


    [SerializeField] private TextMeshProUGUI statsTextMesh;


    private void Update()
    {
        UpdateStatsTextMesh();
    }

    private void UpdateStatsTextMesh()
    {
        statsTextMesh.text =
             GameManager.Instance.GetScore() + "\n" +
             Mathf.Round (GameManager.Instance.GetTime()) + "\n" +
             Mathf.Round(Lander.Instance.GetSpeedX() * 10f) + "\n" +
             Mathf.Round(Lander.Instance.GetSpeedY() * 10f) + "\n" +
             Lander.Instance.GetFuel();
    }

}
