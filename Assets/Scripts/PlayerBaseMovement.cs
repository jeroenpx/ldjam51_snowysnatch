using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBaseMovement : MonoBehaviour
{
    public float speed = 12;
    public float speedMin = 15;
    public float speedMax = 42;
    public float timeToFullSpeed = 60*5;// 5 min
    public float tiltAmount = 22;
    public float tiltPrediction = 5;
    public float transitionDuration = 0.1f;
    public AnimationCurve transitionCurve;
    public float timeSinceSpeedStart;

    public SlopeBuilder slope;
    public Transform cameraPivot;
    public Transform playerPivot;
    public float lookaheadDistance = 15f;
    public Animator playerAnimator;

    public bool moving = false;
    private bool controlsAvailable = false;

    private int currentTrack = 0;
    private int fromTrack = 0;
    private float percentTransition = 1;

    private float smoothTilt = 1;
    public float smoothTiltVelocity = 1;
    public float smoothTiltTime = 1;

    private bool touchingFloor;
    private float upVelocity;
    public float jumpVelocity = 20f;
    public float jumpGravityUp = 0.05f;
    public float jumpGravityDownFactor = 2;

    public Cinemachine.CinemachineImpulseSource dieScreenshake;

    public GameObject retryMessage;

    public TMPro.TMP_Text text;

    private void Start() {
        speed = speedMin;
        timeSinceSpeedStart = 0;
    }

    Vector3 SamplePath(float zStart, float distance) {
        if(percentTransition < 1) {
            float tempPercentTransition = Mathf.Clamp01(percentTransition + (distance + (zStart - transform.position.z))/speed/transitionDuration);
            // Do transition
            float percentTarget = transitionCurve.Evaluate(tempPercentTransition);
            return (1-percentTarget)*slope.At(fromTrack, zStart, distance) + percentTarget*slope.At(currentTrack, zStart, distance);
        } else {
            // Just follow current track
            return slope.At(currentTrack, zStart, distance);
        }
    }

    [ContextMenu("Die")]
    void Die() {
        moving = false;
        playerAnimator.SetTrigger("Die");
        dieScreenshake.GenerateImpulse();

        StartCoroutine(handleRetry());
    }

    IEnumerator handleRetry() {
        yield return new WaitForSeconds(1);
        retryMessage.SetActive(true);

        bool retryTriggered = false;
        while(!retryTriggered) {
            if(Input.GetKeyDown(KeyCode.R)) {
                retryTriggered = true;
            }
            yield return null;
        }

        // Rebuild Level
        transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + 600);
        slope.zFilledWithTracks = transform.position.z + 40;
        Step(true);
        
        // Trigger retry
        moving = true;
        currentTrack = 0;
        fromTrack = 0;
        percentTransition = 1;
        touchingFloor = false;
        playerAnimator.SetBool("InAir", !touchingFloor);
        upVelocity = 0;
        playerAnimator.SetTrigger("Game");
        speed = speedMin;
        timeSinceSpeedStart = 0;
        retryMessage.SetActive(false);
    }

    void Step(bool forceToFloor) {
        float distance = speed * Time.deltaTime;

        slope.UpdatePlayerPosition(transform.position.z);
        Vector3 destination = SamplePath(transform.position.z, distance);

        float gravityShift = 0;
        if(touchingFloor) {
            upVelocity = 0f;
        } else {
            if(upVelocity > 0) {
                upVelocity -= jumpGravityUp*Time.deltaTime;
            } else {
                upVelocity -= jumpGravityUp*Time.deltaTime * jumpGravityDownFactor;
            }
            gravityShift = upVelocity*Time.deltaTime;
        }

        //Vector3 up = Vector3.up * .5f;

        //Debug.DrawLine(transform.position+up, destination+up, Color.yellow);
        transform.position = new Vector3(destination.x, forceToFloor?destination.y:(transform.position.y + gravityShift), destination.z);

        // Possibly move the player down a bit if he is floating (but allow going below the floor!)
        RaycastHit hitInfo;
        if(Physics.SphereCast(destination+Vector3.up*2f, 0.1f, Vector3.down, out hitInfo, 5f, 1 << LayerMask.NameToLayer("Ground"), QueryTriggerInteraction.Ignore)) {
            float groundHeight = hitInfo.point.y;
            if(groundHeight > transform.position.y) {
                // We are underground
                // If it is too much, we die!
                if(groundHeight - transform.position.y - distance > 0.02f) {
                    Die();
                    return;
                } else {
                    // Just go up there
                    transform.position = new Vector3(destination.x, hitInfo.point.y, destination.z);
                    if(upVelocity > 0) {
                        // Still jumping
                    } else {
                        touchingFloor = true;
                        playerAnimator.SetBool("InAir", !touchingFloor);
                    }
                }
            } else {
                // In the air!

                // Are we jumping or falling?
                if(upVelocity > 0 || transform.position.y - groundHeight - distance > .05f) {
                    // Yes
                    // Just leave it like that
                    touchingFloor = false;
                    playerAnimator.SetBool("InAir", !touchingFloor);
                } else if(touchingFloor) {
                    // Keep sticking to the floor
                    transform.position = new Vector3(destination.x, hitInfo.point.y, destination.z);
                } else {
                    // Coming down from falling... Fall the last inches still
                }
            }
        }

        // Look ahead camera
        cameraPivot.localRotation = Quaternion.LookRotation(slope.At(0, transform.position.z, lookaheadDistance)-slope.At(0, cameraPivot.transform.position.z, 0), Vector3.up);

        // Rotate player with ground!
        Vector3 slopeShift = SamplePath(transform.position.z, 1) - SamplePath(transform.position.z, -.5f);
        Quaternion playerOrientation = Quaternion.LookRotation(slopeShift, Vector3.up);

        // Predict going to the right/left
        Vector3 slopeAngle = SamplePath(transform.position.z, tiltPrediction) - SamplePath(transform.position.z, 0);
        Vector3 targetDifference = Quaternion.Inverse(playerOrientation)*slopeAngle;

        // Temporary: do tilt in the code
        tiltAmount = (speed - 5) * 2;// Seems to be speed dependent!
        smoothTilt = Mathf.SmoothDamp(smoothTilt, touchingFloor?-targetDifference.x:0f, ref smoothTiltVelocity, smoothTiltTime);
        Quaternion tilt = Quaternion.AngleAxis(smoothTilt*tiltAmount, Vector3.forward);
        playerPivot.localRotation = playerOrientation*tilt;
    }

    public void MoveRight() {
        controlsAvailable = true;
        fromTrack = currentTrack;
        percentTransition = 0;
        currentTrack = fromTrack + 1;
        StartCoroutine(ToggleDisplaySpeed());
    }

    private IEnumerator ToggleDisplaySpeed() {
        yield return new WaitForSeconds(5);
        text.gameObject.SetActive(true);
    }

    string printSpeed(float speed) {
        string speedStr = ""+Mathf.RoundToInt(speed*10);
        if(speedStr.Length<2) {
            return "0."+speedStr;
        } else {
            return speedStr.Substring(0, speedStr.Length-1)+"."+speedStr.Substring(speedStr.Length-1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(percentTransition < 1) {
            percentTransition += Time.deltaTime / transitionDuration;
            percentTransition = Mathf.Clamp01(percentTransition);
        }
        if(moving) {
            Step(false);

            timeSinceSpeedStart+=Time.deltaTime;
            speed = timeSinceSpeedStart/timeToFullSpeed * (speedMax - speedMin) + speedMin;
            text.text = "current speed: "+printSpeed(speed)+" m/s";

            if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) {
                // Switch track to the right
                if(currentTrack < 1) {
                    if(percentTransition < 1) {
                        // TODO: allow switching while switching!
                        
                    } else {
                        // Easy
                        fromTrack = currentTrack;
                        percentTransition = 0;
                        currentTrack = fromTrack + 1;
                    }
                }
            }
            if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A)) {
                if(currentTrack > -1) {
                    if(percentTransition < 1) {
                        // TODO: allow switching while switching!
                        
                    } else {
                        // Easy
                        fromTrack = currentTrack;
                        percentTransition = 0;
                        currentTrack = fromTrack - 1;
                    }
                }
            }
            if(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W)) {
                // Jump!
                if(touchingFloor) {
                    upVelocity = jumpVelocity;
                    touchingFloor = false;
                    playerAnimator.SetBool("InAir", !touchingFloor);
                }
            }
        }

        float back = 5f;
        float length = 10f;
        Vector3 previousPoint = SamplePath(transform.position.z, 0-back);
        for(int i = 0; i< 20; i++) {
            Vector3 point = SamplePath(transform.position.z, (i+1)/20.0f*length-back);
            Debug.DrawLine(previousPoint+Vector3.up*0.2f, point+Vector3.up*0.2f, Color.green);
            previousPoint = point;
        }
    }
}
