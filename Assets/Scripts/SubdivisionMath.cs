using UnityEngine;
using System.Collections.Generic;

public class SubdivisionMath {
    public static Vector3[] SubDivisionCurve(Vector3[] pointsOriginal, float resolution) {
        List<Vector3> points = new List<Vector3>(pointsOriginal);
        // Adapt resolution to be more in line with "units"
        // (why? because every iteration we get pulled by 2/8 th of the points, while 6/8 keep it the same)
        // So, the distance we are checking is only "2/8" th of the result
        float resolutionSqr = (resolution/4);
        resolutionSqr = resolutionSqr*resolutionSqr;
        
        // Keep subdiving while needed
        while(true) {
            // Step 1: check if we need to subdivide further (and if so, which points?)
            // NOTE: stop faster for things like physics (less detail), but add more detail for e.g. mesh generation
            var doSplit = new bool[points.Count];
            bool hasSplit = false;
            for(int i=0;i<points.Count;i++) {
                if(i==0 || i == points.Count-1) {
                    doSplit[i] = false;
                } else {
                    // Decide whether we should split further this iteration by the thresholding the distance the point would travel by splitting
                    Vector3 pointPrev = points[(i-1+points.Count)%points.Count];
                    Vector3 pointCurr = points[i];
                    Vector3 pointNext = points[(i+1)%points.Count];

                    // Get the smoothness:
                    float smooth = 1.0f;

                    // Current new subcurv position:
                    Vector3 subcurv = (pointPrev+pointCurr*6+pointNext)/8;
                    Vector3 newPos = smooth*subcurv + (1-smooth)*pointCurr;
                    float sqrDist = Vector3.SqrMagnitude(newPos-pointCurr);

                    // Moves enough?
                    doSplit[i] = sqrDist > resolutionSqr;
                    hasSplit = hasSplit || doSplit[i];
                }
            }
            if(!hasSplit) {
                break;
            }

            // NOTE: usually subdivision curves divide EVERYTHING or NOTHING.
            // Not sure how only subdividing some points will affect the convergion to B-Splines...

            // Step 2: do another subdivide iteration (on the relevant points)!
            var newPoints = new List<Vector3>();
            for(int i=0;i<points.Count+1;i++) {
                if(i>0) {
                    // Insert new point
                    if(doSplit[i-1] || doSplit[i%points.Count]) {
                        Vector3 pointPrev = points[i-1];
                        Vector3 pointCurr = points[i%points.Count];
                        newPoints.Add((pointPrev + pointCurr)/2);
                    }
                }

                if(i<points.Count) {
                    if(!doSplit[i]) {
                        // Just copy point
                        newPoints.Add(points[i]);
                    } else {
                        // Ok, so we are a point in the middle!
                        Vector3 pointPrev = points[(i-1+points.Count)%points.Count];
                        Vector3 pointCurr = points[i];
                        Vector3 pointNext = points[(i+1)%points.Count];

                        // Get the smoothness:
                        float smooth = 1.0f;

                        // Current new subcurv position:
                        Vector3 subcurv = (pointPrev+pointCurr*6+pointNext)/8;

                        // However, depending on the smoothness, we should actually not move the point
                        newPoints.Add(smooth*subcurv + (1-smooth)*pointCurr);
                    }
                }
            }

            points = newPoints;
        }
        return points.ToArray();
    }
}