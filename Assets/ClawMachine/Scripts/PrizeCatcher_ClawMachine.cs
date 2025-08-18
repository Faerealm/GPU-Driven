using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrizeCatcher_ClawMachine : MonoBehaviour
{

    [Header("Score Settings")]
    public ParticleSystem coinExplosion;
    public Manager_ClawMovement managerClawMachine;

    public void OnTriggerEnter(Collider other)
    {
        //检测是否掉进奖品收集区
        if (other.GetComponent<Item_ClawMachine>())
        {
            coinExplosion.Play();
            // 添加硬币
            managerClawMachine.playerCoins += other.GetComponent<Item_ClawMachine>().value;
            // 销毁奖品
            Destroy(other.gameObject);
        }
    }
}
