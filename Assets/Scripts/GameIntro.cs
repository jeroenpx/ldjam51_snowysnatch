using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameIntro : MonoBehaviour
{
    public Animator playerAnimator;
    public Animator mainCameraAnimator;
    public GameObject[] menuObjects;

    public Transform playerPlatform;
    public Transform playerAtEndOfAnim;
    public PlayerBaseMovement playerBase;

    private bool gameStartPressed = false;
    private bool gameStarted = false;
    public GameObject gem;
    public Animator golem;

    public Cinemachine.CinemachineImpulseSource stealImpulse;
    public Cinemachine.CinemachineImpulseSource fallImpulse;
    public Cinemachine.CinemachineImpulseSource stepImpulse;
    public AudioSource dropSound;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    IEnumerator gameIntroCoroutine() {
        yield return new WaitForSeconds(6f);

        // Then, disable the main camera
        mainCameraAnimator.gameObject.SetActive(false);

        // Swap the playerplatform to the right position
        playerPlatform.position = playerAtEndOfAnim.position;
        playerAnimator.SetTrigger("Game");
        playerBase.moving = true;
    }

    IEnumerator destroyGemCoroutine() {
        yield return new WaitForSeconds(5.3f);
        Destroy(gem);
        yield return new WaitForSeconds(.6f);
        stealImpulse.GenerateImpulse();
        golem.SetTrigger("GemStolen");
        yield return new WaitForSeconds(0.9f);
        playerBase.MoveRight();
        yield return new WaitForSeconds(0.2f);
        fallImpulse.GenerateImpulse();
        dropSound.Play();
        yield return new WaitForSeconds(3f);
        /*stepImpulse.GenerateImpulse();
        yield return new WaitForSeconds(1.2f);
        for(int i=0;i<20;i++) {
            stepImpulse.GenerateImpulse();
            yield return new WaitForSeconds(.9f);
        }*/
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!gameStartPressed) {
            if(Input.GetKeyDown(KeyCode.Space)) {
                // Start Game!
                gameStartPressed = true;

                // Start Intro
                playerAnimator.SetTrigger("Intro");
                mainCameraAnimator.SetTrigger("Intro");

                // Hide menu
                for(int i = 0; i<menuObjects.Length;i++) {
                    menuObjects[i].SetActive(false);
                }

                StartCoroutine(gameIntroCoroutine());
                StartCoroutine(destroyGemCoroutine());

                // TODO:
                // -> switch camera (side view)
                // -> switch camera (jump follow)
                // -> switch camera (rock)
                // -> switch camera: normal gameplay
            }
        }
    }
}
