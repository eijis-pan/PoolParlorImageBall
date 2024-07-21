using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

public class ImageBallManager : UdonSharpBehaviour
{
    [SerializeField] public GameObject[] imageBalls;
    private GameObject[] targetGuideline;
    private GameObject[] followGuideline;

    private const float k_BALL_RADIUS = 0.03f;
    
    [Space(10)]
    [Header("reference of billiards module")]
    [SerializeField] public BilliardsModule table;
    
    private int repositionCount;
    private bool[] repositioning;
    
    private int[] targetIndex;
    
    [UdonSynced] [NonSerialized] public Vector3[] ballsPSynced;
    [UdonSynced] [NonSerialized] public int[] targetIndexSynced;
    [UdonSynced] [NonSerialized] public Vector2[] targetGuideSynced;
    [UdonSynced] [NonSerialized] public Vector2[] followGuideSynced;
    
    public override void OnDeserialization()
    {
        for (int i = 0; i < imageBalls.Length; i++)
        {
            if (i < ballsPSynced.Length)
            {
                imageBalls[i].transform.position = ballsPSynced[i];
            }
        }        
        for (int i = 0; i < targetGuideline.Length; i++)
        {
            if (i < targetGuideSynced.Length)
            {
                float deg = targetGuideSynced[i].x;
                float scale = targetGuideSynced[i].y;
                var guide = targetGuideline[i];
                if (targetIndexSynced[i] < 0)
                {
                    guide.SetActive(false);
                }
                else
                {
                    guide.SetActive(true);
                    var localScale = guide.transform.localScale;
                    localScale.x = scale;
                    guide.transform.localScale = localScale;
                    guide.transform.localEulerAngles = new Vector3(0.0f,  deg, 0.0f);
                    guide.transform.position = table.balls[targetIndexSynced[i]].transform.position;
                }
            }
        }
        for (int i = 0; i < followGuideline.Length; i++)
        {
            if (i < followGuideSynced.Length)
            {
                float deg = followGuideSynced[i].x;
                float scale = followGuideSynced[i].y;
                var guide = followGuideline[i];
                if (scale < 0)
                {
                    guide.SetActive(false);
                }
                else
                {
                    guide.SetActive(true);
                    var localScale = guide.transform.localScale;
                    localScale.x = scale;
                    guide.transform.localScale = localScale;
                    guide.transform.localEulerAngles = new Vector3(0.0f,  deg, 0.0f);
                }
                guide.transform.position = imageBalls[i].transform.position;
            }
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (ReferenceEquals(null, player))
        {
            return;
        }
        
        if (player.isLocal)
        {
            return;
        }
        
        if (ReferenceEquals(null, Networking.LocalPlayer))
        {
            return;
        }
        
        if (Networking.LocalPlayer.isMaster)
        {
            RequestSerialization();
        }
    }
    
    private void OnEnable()
    {
        ballsPSynced = new Vector3[imageBalls.Length];
        targetGuideline = new GameObject[imageBalls.Length];
        followGuideline = new GameObject[imageBalls.Length];
        targetIndex = new int[targetGuideline.Length];
        for (int i = 0; i < targetGuideline.Length; i++)
        {
            targetIndex[i] = -1;
        }
        targetIndexSynced = new int[targetGuideline.Length];
        targetGuideSynced = new Vector2[targetGuideline.Length];
        followGuideSynced = new Vector2[followGuideline.Length];
        
        for (int i = 0; i < imageBalls.Length; i++)
        {
            imageBalls[i].GetComponentInChildren<ImageBallRepositioner>(true)._Init(this, i);
            targetGuideline[i] = imageBalls[i].transform.Find("target_guide").gameObject;
            followGuideline[i] = imageBalls[i].transform.Find("follow_guide").gameObject;
        }
        for (int i = 0; i < targetGuideline.Length; i++)
        {
            targetGuideline[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material.SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
        }
        for (int i = 0; i < followGuideline.Length; i++)
        {
            followGuideline[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material.SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
        }

        _Init();
    }

    public void _Init()
    {
        repositioning = new bool[table.balls.Length];

        _OnGameStarted();
    }
    
    public void _OnGameStarted()
    {
        repositionCount = 0;
        Array.Clear(repositioning, 0, repositioning.Length);
    }
    
    private void Update()
    {
        this._Tick();
    }

    public void _Tick()
    {
        if (repositionCount == 0) return;

        Vector3 k_pR = (Vector3)table.currentPhysicsManager.GetProgramVariable("k_pR");
        Vector3 k_pO = (Vector3)table.currentPhysicsManager.GetProgramVariable("k_pO");
        Transform transformSurface = (Transform)table.currentPhysicsManager.GetProgramVariable("transform_Surface");
        for (int i = 0; i < repositioning.Length; i++)
        {
            if (!repositioning[i]) continue;

            GameObject imageBall = imageBalls[i];
           
            Transform pickupTransform = imageBall.transform.GetChild(0);
            
            float maxX = k_pR.x;

            Vector3 boundedLocation = table.transform.InverseTransformPoint(pickupTransform.position);
            boundedLocation.x = Mathf.Clamp(boundedLocation.x, -k_pR.x, maxX);
            boundedLocation.z = Mathf.Clamp(boundedLocation.z, -k_pO.z, k_pO.z);
            boundedLocation.y = 0.0f;
            
            // ensure no collisions
            bool collides = false;
            int collidedBall = -1;
            Collider[] colliders = Physics.OverlapSphere(transformSurface.TransformPoint(boundedLocation), k_BALL_RADIUS);
            for (int j = 0; j < colliders.Length; j++)
            {
                if (colliders[j] == null) continue;
                
                GameObject collided = colliders[j].gameObject;
                if (collided == imageBall) continue;
                
                collidedBall = Array.IndexOf(table.balls, collided);
                if (0 < collidedBall)
                {
                    Boolean aleadyCollided = false;
                    for (int k = 0; k < targetIndex.Length; k++)
                    {
                        if (i == k)
                        {
                            continue;
                        }

                        if (targetIndex[k] == collidedBall)
                        {
                            aleadyCollided = true;
                        }
                    }

                    if (aleadyCollided)
                    {
                        continue;
                    }
                    
                    collides = true;
                    break;
                }
                
                if (collided.name == "table" || collided.name == "glass" || collided.name == ".4BALL_FILL")
                {
                    collides = true;
                    break;
                }
            }

            float targetGuidelineAdjust = 180; //90;
            
            if (targetIndex[i] != collidedBall)
            {
                if (0 < collidedBall)
                {
                    Vector3 ghost2Target = table.balls[collidedBall].transform.position - imageBalls[i].transform.position;
                    Vector3 cue2target = table.balls[collidedBall].transform.position - table.balls[0].transform.position;
                    Vector3 cue2ghost = imageBalls[i].transform.position - table.balls[0].transform.position;
                    
                    float g2tRad = -Mathf.Atan2(ghost2Target.z, ghost2Target.x);
                    float c2tRad = -Mathf.Atan2(cue2target.z, cue2target.x);
                    float c2gRad = -Mathf.Atan2(cue2ghost.z, cue2ghost.x);

                    float g2tDeg = g2tRad * Mathf.Rad2Deg;
                    float c2tDeg = c2tRad * Mathf.Rad2Deg;
                    float c2gDeg = c2gRad * Mathf.Rad2Deg;
                    
                    targetGuideline[i].transform.position = table.balls[collidedBall].transform.position;
                    targetGuideline[i].transform.localEulerAngles = new Vector3(0.0f, g2tDeg + targetGuidelineAdjust, 0.0f);
                    
                    float ct2gtDiff = g2tDeg - c2tDeg;
                    
                    float followGuidelineAdjust2 = 90; //180;
                    if ((-180 <= ct2gtDiff && ct2gtDiff < 0) || 180 <= ct2gtDiff)
                    {
                        followGuidelineAdjust2 = 270; //0;
                    }
                    followGuideline[i].transform.localEulerAngles = new Vector3(0.0f, g2tDeg + followGuidelineAdjust2, 0.0f);
                    
                    float followScale = 0.06f;
                    float targetScale = 0.06f;
                    float gt2cgDiff = c2gDeg - g2tDeg;
                    if (gt2cgDiff < -270 || (-90 <= gt2cgDiff && gt2cgDiff < 90) || 270 <= gt2cgDiff)
                    {
                        float gt2cgDiff2 = gt2cgDiff;
                        if (270 <= gt2cgDiff2)
                        {
                            gt2cgDiff2 = 360 - gt2cgDiff2;
                            if (gt2cgDiff2 < 0)
                            {
                                gt2cgDiff2 = 180 - gt2cgDiff2;
                            }
                        }
                        else if (gt2cgDiff2 < 0)
                        {
                            if (gt2cgDiff2 <= -270)
                            {
                                gt2cgDiff2 += 360;
                            }
                            if (gt2cgDiff2 < 0)
                            {
                                gt2cgDiff2 *= -1;
                            }
                        }
                        float ratio = gt2cgDiff2 / 90;
                        followScale = 0.04f * (ratio);
                        targetScale = 0.04f * (1 - ratio);
                    }

                    var followLocalScale = followGuideline[i].transform.localScale;
                    followLocalScale.x = followScale;
                    followGuideline[i].transform.localScale = followLocalScale;
                    
                    var targetLocalScale = targetGuideline[i].transform.localScale;
                    targetLocalScale.x = targetScale;
                    targetGuideline[i].transform.localScale = targetLocalScale;
                }
                targetIndex[i] = collidedBall;
            }

            if (!collides)
            {
                // no collisions, we can update the position and reset the pickup
                var pos = table.transform.TransformPoint(boundedLocation);
                pos.y = 0.8606015f;;

                imageBalls[i].transform.position = pos;

                targetGuideline[i].transform.position = pos;
                followGuideline[i].transform.position = pos;
                
                pickupTransform.localPosition = Vector3.zero;
                pickupTransform.localRotation = Quaternion.identity;
                
                targetGuideline[i].SetActive(false);
                followGuideline[i].SetActive(false);
            }
            else
            {
                targetGuideline[i].SetActive(true);

                if (0 < targetIndex[i])
                {
                    Vector3 q = table.balls[targetIndex[i]].transform.position - imageBalls[i].transform.position;
                    Vector3 q3 = table.balls[0].transform.position - imageBalls[i].transform.position;
                    float targetDeg = -Mathf.Atan2(q.z, q.x) * Mathf.Rad2Deg;
                    float cueDeg2 = (Mathf.Atan2(q3.z, q3.x) * Mathf.Rad2Deg) + targetDeg;
                    if ((-270 < cueDeg2 && cueDeg2 < -90)||(90 < cueDeg2 && cueDeg2 < 270))
                    {
                        followGuideline[i].SetActive(true);
                    }
                    else
                    {
                        followGuideline[i].SetActive(false);
                    }
                }
                else
                {
                    followGuideline[i].SetActive(true);
                }
            }
        }
    }

    public void _BeginReposition(ImageBallRepositioner grip)
    {
        int idx = grip.idx;
        if (repositioning[idx])
        {
            return;
        }
        
        repositioning[idx] = true;
        repositionCount++;

        if (!Networking.IsOwner(imageBalls[idx]))
        {
            Networking.SetOwner(Networking.LocalPlayer, imageBalls[idx]);
        }
    }

    public void _EndReposition(ImageBallRepositioner grip)
    {
        int idx = grip.idx;
        if (!repositioning[idx]) return;
        
        repositioning[idx] = false;
        repositionCount--;

        grip._Reset();
        
        for (int i = 0; i < imageBalls.Length; i++)
        {
            if (i < ballsPSynced.Length)
            {
                ballsPSynced[i] = imageBalls[i].transform.position;
            }
        }        
        for (int i = 0; i < targetGuideline.Length; i++)
        {
            if (i < targetGuideSynced.Length)
            {
                var guide = targetGuideline[i];
                targetGuideSynced[i].x = guide.transform.localEulerAngles.y;
                targetGuideSynced[i].y = guide.transform.localScale.x;
                targetIndexSynced[i] = targetIndex[i];
            }
        }
        for (int i = 0; i < followGuideline.Length; i++)
        {
            if (i < followGuideSynced.Length)
            {
                var guide = followGuideline[i];
                followGuideSynced[i].x = guide.transform.localEulerAngles.y;
                followGuideSynced[i].y = guide.activeSelf ? guide.transform.localScale.x : -1;
            }
        }
        
        this.RequestSerialization();
    }
}