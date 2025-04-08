using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;
using Spine;

public class AimSystem : MonoBehaviour
{
    [Header("Spine")]
    public SkeletonAnimation skeletonAnimation;
    public string torsoBoneName = "body2";
    public string torsoBone2Name = "body3";
    public string leftArmBoneName = "armL";
    public string leftArm2BoneName = "armL2";
    private float torsoAimAngle;

    private Bone torsoBone;
    private Bone torsoBone2;
    private Bone leftArmBone;
    private Bone leftArm2Bone;
    
    private float defaultTorsoRotation;
    private float defaultTorso2Rotation;
    private float defaultLeftArmRotation;
    private float defaultLeftArm2Rotation;

    [Header("References")]
    public Transform arrowSpawnPoint;
    public GameObject arrowPrefab;

    [Header("Settings")]
    public float maxPullDistance = 3f;
    public float forceMultiplier = 10f;
    public float dotSpacing = 0.1f;
    public int dotCount = 20;
    
    [Header("Trajectory Dots")]
    public GameObject dotPrefab;
    public Transform dotParent;
    private List<GameObject> trajectoryDots = new List<GameObject>();

    private Vector2 startTouchPos;
    private Camera cam;
    private bool isAiming;

    void Start()
    {
        cam = Camera.main;
        HideDots();

        if (skeletonAnimation != null)
        {
            torsoBone = skeletonAnimation.Skeleton.FindBone(torsoBoneName);
            torsoBone2 = skeletonAnimation.Skeleton.FindBone(torsoBone2Name);
            leftArmBone = skeletonAnimation.Skeleton.FindBone(leftArmBoneName);
            leftArm2Bone = skeletonAnimation.Skeleton.FindBone(leftArm2BoneName);
            skeletonAnimation.UpdateLocal += ApplyTorsoRotation;
            
            if (torsoBone != null) defaultTorsoRotation = torsoBone.Rotation;
            if (torsoBone2 != null) defaultTorso2Rotation = torsoBone2.Rotation;
            if (leftArmBone != null) defaultLeftArmRotation = leftArmBone.Rotation;
            if (leftArm2Bone != null) defaultLeftArm2Rotation = leftArm2Bone.Rotation;
        }
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartAiming(cam.ScreenToWorldPoint(Input.mousePosition));
        }
        else if (Input.GetMouseButton(0) && isAiming)
        {
            ContinueAiming(cam.ScreenToWorldPoint(Input.mousePosition));
        }
        else if (Input.GetMouseButtonUp(0) && isAiming)
        {
            Release(cam.ScreenToWorldPoint(Input.mousePosition));
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        Vector2 worldPos = cam.ScreenToWorldPoint(touch.position);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                StartAiming(worldPos);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (isAiming) ContinueAiming(worldPos);
                break;
            case TouchPhase.Ended:
                if (isAiming) Release(worldPos);
                break;
        }
    }

    void StartAiming(Vector2 touchPos)
    {
        isAiming = true;
        startTouchPos = touchPos;

        if (!IsPlayingAnimation("attack_start"))
        {
            var track = skeletonAnimation.AnimationState.SetAnimation(0, "attack_start", false);
            track.TimeScale = 1f;
            track.Complete += entry =>
            {
                entry.TimeScale = 0f;
            };
        }

        ShowDots();
    }

    void ContinueAiming(Vector2 currentTouchPos)
    {
        Vector2 pullVector = Vector2.ClampMagnitude(startTouchPos - currentTouchPos, maxPullDistance);
        UpdateTrajectory(pullVector);
        RotateTorso(pullVector);
    }

    void Release(Vector2 releaseTouchPos)
    {
        isAiming = false;
        Vector2 pullVector = Vector2.ClampMagnitude(startTouchPos - releaseTouchPos, maxPullDistance);
        ShootArrow(pullVector);
    
        var track = skeletonAnimation.AnimationState.SetAnimation(0, "attack_finish", false);
        track.TimeScale = 1f;
        track.Complete += entry =>
        {
            entry.TimeScale = 0f; // Pause at the end
        };

        HideDots();
    }

    void ShootArrow(Vector2 direction)
    {
        GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.identity);
        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();


        Vector2 velocity = direction * forceMultiplier;
        rb.velocity = velocity;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        
    }

    void UpdateTrajectory(Vector2 pullVector)
    {
        Vector2 velocity = pullVector * forceMultiplier;
        for (int i = 0; i < trajectoryDots.Count; i++)
        {
            float t = i * dotSpacing;
            Vector2 pos = (Vector2)arrowSpawnPoint.position + velocity * t + 0.5f * Physics2D.gravity * t * t;
            trajectoryDots[i].transform.position = pos;
        }
    }

    void RotateTorso(Vector2 direction)
    {
        if (torsoBone == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        torsoAimAngle = angle;
    }
    
    void ApplyTorsoRotation(ISkeletonAnimation animated)
    {
        if (!isAiming)
        {
            if (torsoBone != null) torsoBone.Rotation = defaultTorsoRotation;
            if (torsoBone2 != null) torsoBone2.Rotation = defaultTorso2Rotation;
            if (leftArmBone != null) leftArmBone.Rotation = defaultLeftArmRotation;
            if (leftArm2Bone != null) leftArm2Bone.Rotation = defaultLeftArm2Rotation;
            return;
        }

        if (torsoBone != null) torsoBone.Rotation = torsoAimAngle;
        if (torsoBone2 != null) torsoBone2.Rotation = torsoAimAngle;
        if (leftArmBone != null) leftArmBone.Rotation = torsoAimAngle;
        if (leftArm2Bone != null) leftArm2Bone.Rotation = torsoAimAngle;
    }

    void ShowDots()
    {
        if (trajectoryDots.Count > 0) return;

        for (int i = 0; i < dotCount; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotParent);
            dot.SetActive(true);
            trajectoryDots.Add(dot);
        }
    }

    void HideDots()
    {
        foreach (var dot in trajectoryDots)
            Destroy(dot);

        trajectoryDots.Clear();
    }
    
    bool IsPlayingAnimation(string animationName)
    {
        var current = skeletonAnimation.AnimationState.GetCurrent(0);
        return current != null && current.Animation.Name == animationName;
    }
}
