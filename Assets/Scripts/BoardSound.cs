using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSound : MonoBehaviour
{
    public Transform source;

    public AudioSource boardsound;

    float volume = 0;
    public float speedFade = 10;
    public float targetVolume = 1;
    public float touchDistance = .3f;

    private Vector3 lastPosition;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool boardOnGround = false;
        if(Vector3.Distance(lastPosition, source.position) > Time.deltaTime*5 && Physics.Raycast(source.position - source.forward, source.forward, 1+touchDistance, 1 << LayerMask.NameToLayer("Ground"), QueryTriggerInteraction.Ignore)) {
            boardOnGround = true;
        }

        volume = Mathf.Clamp01(volume + (boardOnGround?1:-1)*Time.deltaTime*speedFade);

        boardsound.volume = volume * targetVolume;

        lastPosition = source.position;
    }
}
