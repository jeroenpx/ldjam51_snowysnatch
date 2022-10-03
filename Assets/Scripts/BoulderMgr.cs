using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderMgr : MonoBehaviour
{
    public static BoulderMgr instance;

    public float timer = 0;

    void Awake() {
        instance = this;
    }

    private void Update() {
        timer+=Time.deltaTime;
    }

    public bool ShouldTriggerBoulder() {
        if(timer > 10) {
            timer = 0;
            return true;
        } else {
            return false;
        }
    }
}
