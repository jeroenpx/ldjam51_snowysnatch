using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectiblesFinder : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Gem") {
            Destroy(other.gameObject);
        }
    }

}
