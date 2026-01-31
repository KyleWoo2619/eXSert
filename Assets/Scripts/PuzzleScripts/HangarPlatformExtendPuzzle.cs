/*
    Written by Brandon Wahl

    This script manages the platform extender puzzle in the hangar level. Here once the designated interact point is click,
    the platform will extend outward or inward respectively.
*/

using UnityEngine;

public class HangarPlatformExtendPuzzle : PuzzlePart
{
    [SerializeField] private float lerpSpeed = 10f;
    private bool isExtending = false;
    private Vector3 startPos;
    private Vector3 targetPos;

    protected Vector3 origin;

    private void Awake()
    {
        origin = this.transform.position;
    }

    // Extends platform out to desired point
    public override void StartPuzzle()
    {
        if(!isCompleted)
        {
            startPos = origin;
            targetPos = new Vector3(startPos.x - 10, startPos.y, startPos.z);
            isExtending = true;
            isCompleted = true;
        }
    }

    // If the platform is already extended, if it is clicked again it will revert
    public override void EndPuzzle()
    {
        if(isCompleted)
        {
            startPos = this.transform.position;
            targetPos = origin;
            isExtending = true;
            isCompleted = false;
        }
    }

    // to reduce performance impact, change this to be a coroutine
    private void Update()
    {
        if (isExtending)
        {
            float t = Mathf.Clamp01(lerpSpeed * Time.deltaTime);
            this.transform.position = Vector3.Lerp(this.transform.position, targetPos, t);

            if (Vector3.Distance(this.transform.position, targetPos) < 0.01f)
            {
                this.transform.position = targetPos;
                isExtending = false;
            }
        }
    }   
}
