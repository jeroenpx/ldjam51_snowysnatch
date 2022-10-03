using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boulder : MonoBehaviour
{
    public GameObject displayPart;
    public Animator throwAnim;
    public Cinemachine.CinemachineImpulseSource dropImpulse;
    public AudioSource dropSound;

    private IEnumerator throwEffect () {
        yield return new WaitForSeconds(1);
        dropImpulse.GenerateImpulse();
        dropSound.Play();
    }

    void OnTriggerEnter(Collider other) {
        if(other.gameObject.tag == "Player") {
            if(BoulderMgr.instance.ShouldTriggerBoulder()) {
                displayPart.SetActive(true);
                throwAnim.SetTrigger("Throw");
                StartCoroutine(throwEffect ());
            } else {
                Destroy(gameObject);
            }
        }
    }
}
