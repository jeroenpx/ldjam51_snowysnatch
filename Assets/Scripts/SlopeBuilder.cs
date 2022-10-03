using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlopeBuilder : MonoBehaviour
{
    // Available Pieces
    public Slope startPiece;

    public Slope[] availablePieces;

    public Transform[] availableTracks;
    private float longestTrack;
    public float zFilledWithTracks;

    public float spaceAfterTrack = 30;

    public float trackItemsScale = 2;

    public float knownSpeed = 12;

    // Renderers
    public LineRenderer[] lines;

    // Settings (lookahead in meters)
    public float lookahead = 1000;
    public float keepbehind = 200;
    public float curveResolution = 1;
    public float keepbackwardscurve = 20;
    public float laneWidth = 3f;
    public float distanceSinceLastBoulder = 0;

    // Line intro
    public AnimationCurve lineIntro;
    public float lineIntroLength = 60f;
    public float lineIntroHeight = 10f;


    // Current State
    private List<Slope> slopePieces;
    private List<Vector3> curveAheadBlocky;
    private List<Vector3> curveAhead;
    private List<GameObject> trackItems;
    private List<float> distanceToNext;
    private int lastUsedIndex = -1;
    private int lastUsedTrackIndex = -1;
    

    /** Spawn a new slope at the end */
    private void AppendSlope() {
        // Figure out which slope to instantiate
        int idx = Random.Range(0, availablePieces.Length-(lastUsedIndex>=0?1:0));
        if(lastUsedIndex>=0) {
            if(idx>=lastUsedIndex) {
                idx++;
            }
        }
        lastUsedIndex = idx;
        GameObject template = availablePieces[idx].gameObject;

        // Where to put it?
        Vector3 endOfSlope = slopePieces[slopePieces.Count-1].GetGlobalSpaceEndPoint();
        Vector3 spawnShift = availablePieces[idx].GetGlobalSpawnShift();

        GameObject newSlopeObj = GameObject.Instantiate(template, endOfSlope+spawnShift, Quaternion.identity);
        Slope newSlope = newSlopeObj.GetComponent<Slope>();
        slopePieces.Add(newSlope);

        AppendSlopeCurve(newSlope);
    }

    private float GetLengthOfTrack(Transform trackPrefab) {
        float maxDepth = 0f;
        for(int i=0;i<trackPrefab.childCount;i++) {
            Transform trackItem = trackPrefab.GetChild(i);
            if(trackItem.name == "Preview") {
                continue;
            }
            int track = Mathf.RoundToInt(trackItem.localPosition.x);
            if(track < 0 || track >= 3) {
                Debug.Log("Found invalid track in "+trackPrefab.name);
                continue;
            }
            float distance = trackItem.localPosition.z;
            if(distance > maxDepth) {
                maxDepth = distance;
            }
        }
        return maxDepth*trackItemsScale + 0.9f;// add some extra space (half a grid cell)
    }

    private void AppendTrack(Transform trackPrefab) {
        for(int i=0;i<trackPrefab.childCount;i++) {
            Transform trackItem = trackPrefab.GetChild(i);
            if(trackItem.name == "Preview") {
                continue;
            }
            int track = Mathf.RoundToInt(trackItem.localPosition.x);
            if(track < 0 || track >= 3) {
                Debug.Log("Found invalid track in "+trackPrefab.name);
                continue;
            }
            float distance = trackItem.localPosition.z*trackItemsScale;
            Vector3 position = At(track-1, zFilledWithTracks, distance);
            Vector3 forward = At(track-1, zFilledWithTracks, distance+0.1f);
            Quaternion itemAngle = Quaternion.LookRotation(forward-position, Vector3.up);

            GameObject newTrackItem = Instantiate(trackItem.gameObject, position+new Vector3(trackItem.localPosition.x-track, trackItem.localPosition.y, 0)*trackItemsScale, trackItem.localRotation*itemAngle);
            newTrackItem.transform.localScale = Vector3.one* trackItemsScale;
            trackItems.Add(newTrackItem);

        }
        zFilledWithTracks += GetLengthOfTrack(trackPrefab) + spaceAfterTrack;
        distanceSinceLastBoulder += GetLengthOfTrack(trackPrefab) + spaceAfterTrack;
    }
    

    /** Try to append a track - assuming there is enough space! */
    private void AppendTrack() {
        int idx = Random.Range(0, availableTracks.Length-(lastUsedTrackIndex>=0?1:0));
        if(lastUsedTrackIndex>=0) {
            if(idx>=lastUsedTrackIndex) {
                idx++;
            }
        }
        lastUsedTrackIndex = idx;
        AppendTrack(availableTracks[idx]);
    }

    private void AppendSlopeCurve(Slope newSlope) {
        curveAheadBlocky.AddRange(newSlope.GetPoints());
        curveAheadBlocky.RemoveAt(curveAheadBlocky.Count-1);
    }

    private void UpdateCalculatedCurves() {
        distanceToNext.Clear();
        curveAhead.Clear();
        curveAhead.AddRange(SubdivisionMath.SubDivisionCurve(curveAheadBlocky.ToArray(), curveResolution));
        for(int i=0;i<curveAhead.Count-1;i++) {
            distanceToNext.Add(Vector3.Distance(curveAhead[i+1], curveAhead[i]));
        }
    }

    private void BuildAroundPlayer(float playerZ) {
        Vector3 endOfSlope = slopePieces[slopePieces.Count-1].GetGlobalSpaceEndPoint();
        float currentLookahead = endOfSlope.z - playerZ;
        bool changed = false;
        while(currentLookahead < lookahead) {
            AppendSlope();
            endOfSlope = slopePieces[slopePieces.Count-1].GetGlobalSpaceEndPoint();
            currentLookahead = endOfSlope.z - playerZ;
            changed = true;
        }

        if(changed) {
            // Cleanup slope prefabs!
            int lastCleanupIdx = -1;
            for(int i=0;i<slopePieces.Count;i++) {
                if(slopePieces[i].GetGlobalSpaceEndPoint().z < playerZ - keepbehind) {
                    Destroy(slopePieces[i].gameObject);
                    lastCleanupIdx = i;
                } else {
                    break;
                }
            }
            if(lastCleanupIdx>=0) {
                slopePieces.RemoveRange(0, lastCleanupIdx+1);
            }

            // Cleanup points
            int lastCleanupCurveIdx = -1;
            for(int i=0;i<curveAheadBlocky.Count;i++) {
                if(curveAheadBlocky[i].z < playerZ-keepbackwardscurve) {
                    lastCleanupCurveIdx = i;
                } else {
                    break;
                }
            }
            if(lastCleanupCurveIdx>=0) {
                curveAheadBlocky.RemoveRange(0, lastCleanupCurveIdx+1);
            }

            List<GameObject> copiedTrackItems = new List<GameObject>();
            for(int i=0;i<trackItems.Count;i++) {
                if(trackItems[i] != null) {
                    if(trackItems[i].transform.position.z >= playerZ-keepbehind) {
                        copiedTrackItems.Add(trackItems[i]);
                    } else {
                        Destroy(trackItems[i]);
                    }
                }
            }
            trackItems = copiedTrackItems;

            // Recalculate stuff
            UpdateCalculatedCurves();
            RedrawLines();
        }

        // Possibly append tracks as well
        while(zFilledWithTracks+longestTrack + 50 /* not sure why this doesn't work, just add +50 */ < curveAheadBlocky[curveAheadBlocky.Count-1].z) {
            AppendTrack();
        }
    }

    private void RedrawLines() {
        Vector3[] lanePoints = new Vector3[curveAhead.Count];
        for(int lineIdx = 0; lineIdx < 4; lineIdx ++) {
            for(int i = 0; i<lanePoints.Length;i++) {
                float yShift = (lineIntro.Evaluate(Mathf.Clamp01(curveAhead[i].z/lineIntroLength))-1)*lineIntroHeight;
                lanePoints[i] = curveAhead[i] + new Vector3(laneWidth*(-1.5f+lineIdx), yShift, 0);
            }
            LineRenderer lineRender = lines[lineIdx];
            lineRender.positionCount = curveAhead.Count;
            lineRender.SetPositions(lanePoints);
            lineRender.widthMultiplier = .1f;
        }
    }

    private void Start()
    {
        // Init
        curveAheadBlocky = new List<Vector3>();
        curveAhead = new List<Vector3>();
        distanceToNext = new List<float>();
        slopePieces = new List<Slope>();
        trackItems = new List<GameObject>();

        // Calculate longest track
        longestTrack = 0f;
        for(int i = 0; i< availableTracks.Length;i++) {
            float lengthOfTrack = GetLengthOfTrack(availableTracks[i]);
            if(lengthOfTrack> longestTrack) {
                longestTrack = lengthOfTrack;
            }
        }

        // Add initial slope to the list
        slopePieces.Add(startPiece);
        AppendSlopeCurve(startPiece);

        // Build the begin slope (assume player starts at 0)
        BuildAroundPlayer(0);
    }
    
    private void Update() {
        // Debug draw the whole slope curve (smooth)
        Vector3 shiftUp = Vector3.up;

        Vector3 previousPoint = curveAhead[0];
        for(int i=1;i<curveAhead.Count;i++) {
            Vector3 newPoint = curveAhead[i];
            Debug.DrawLine(previousPoint+shiftUp, newPoint+shiftUp, Color.red);
            previousPoint = newPoint;
        }
    }

    public void UpdatePlayerPosition(float z) {
        BuildAroundPlayer(z);
    }

    public Vector3 At(int curve, float currentZ, float distance) {
        // Find segment we start in
        int startIdx = 0;
        for(int i=1;i<curveAhead.Count;i++) {
            if(curveAhead[i].z>currentZ) {
                startIdx = i-1;
                break;
            }
        }

        // Correct distance so we start at a segment
        float percentStartFirstSegment = (currentZ - curveAhead[startIdx].z) / (curveAhead[startIdx+1].z - curveAhead[startIdx].z);
        percentStartFirstSegment = Mathf.Clamp01(percentStartFirstSegment);
        distance += percentStartFirstSegment * distanceToNext[startIdx];

        // Loop through the complete segments we pass
        while(distance > distanceToNext[startIdx]) {
            distance -= distanceToNext[startIdx];
            startIdx++;
        }

        // Finally we end up somewhere in the next segment
        float percentInLastSegment = distance / distanceToNext[startIdx];

        // Interpolate final position
        return curveAhead[startIdx] * (1-percentInLastSegment) + curveAhead[startIdx+1] * percentInLastSegment + new Vector3(laneWidth*curve, 0, 0);
    }
}
