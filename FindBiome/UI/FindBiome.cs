using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class FindBiome : MonoBehaviour
{
    [SerializeField] Button BatonTP;
    [SerializeField] Button btnFind;
    [SerializeField] TMP_InputField inputos;
    [SerializeField] TMP_Text statuso;

    [Space]

    [SerializeField] TMP_InputField inputX;
    [SerializeField] TMP_InputField inputY;
    [SerializeField] TMP_InputField inputZ;
    [SerializeField] Button BatonMove;


    Camera main;
    Vector3 targetPos;

    private void Awake()
    {
        btnFind.onClick.AddListener(BtnFind_Clicked);
        BatonTP.onClick.AddListener(TP_Clicked);
        BatonMove.onClick.AddListener(Move_Clicked);

        BatonTP.gameObject.SetActive(false);

        main = Camera.main;
    }

    private void Move_Clicked()
    {
        var x = int.Parse(inputX.text);
        var y = int.Parse(inputY.text);
        var z = int.Parse(inputZ.text);

        main.transform.position = new Vector3(x, y, z);
    }

    private void BtnFind_Clicked()
    {
        btnFind.gameObject.SetActive(false);

        var generation = WorldGenerator.Inst.procedural;
        BiomeNoiseResult[] ebala = new BiomeNoiseResult[WorldGenerator.Inst.procedural.biomes.Length];

        StartCoroutine(Async());

        IEnumerator Async()
        {
            var startZ = Mathf.RoundToInt(main.transform.position.z);
            var startX = Mathf.RoundToInt(main.transform.position.x);
            var startY = Mathf.RoundToInt(main.transform.position.y);

            if (int.TryParse(inputos.text, out var targetBiomeIdx))
            {
                while (true)
                {
                    var biomeIdx = generation.GetBiomeIndex(startX, startY, startZ, out var biome, ref ebala);
                    if (biomeIdx == targetBiomeIdx)
                    {
                        targetPos.Set(startX, startY, startZ);
                        BatonTP.gameObject.SetActive(true);

                        break;
                    }

                    startZ++;
                    statuso.SetText($"Зырим {startX} {startY} {startZ}");

                    yield return new WaitForEndOfFrame();
                }
            }
            else
            {
                statuso.SetText("Ты Ебанько? Индекс ебани");
            }
        }
    }

    private void TP_Clicked()
    {
        main.transform.position = targetPos;
        btnFind.gameObject.SetActive(true);
        BatonTP.gameObject.SetActive(false);
    }
}
