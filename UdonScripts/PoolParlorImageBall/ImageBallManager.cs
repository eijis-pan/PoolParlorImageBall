//#define TKCH_DEBUG_IMAGE_BALLS

using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ImageBallManager : UdonSharpBehaviour
{
    [SerializeField] public GameObject[] imageBalls;
    [SerializeField] public GameObject imageBallInMirrorParent;
    private GameObject[] targetGuideline;
    private GameObject[] followGuideline;
    private Material[] targetGuideDisplayMaterial;
    private Material[] followGuideDisplayMaterial;
    private float guideScaleBase = 0.04f; // carom 0.2f bank 0.12f, normal 0.04f

    private const float ballMeshDiameter = 0.06f;//the ball's size as modeled in the mesh file
    private float k_BALL_RADIUS = 0.03f;
    private float maxX;
    private float maxZ;
    private Transform transformSurface;
    // private readonly int _Floor = Shader.PropertyToID("_Floor");    
    // private readonly int _Dims = Shader.PropertyToID("_Dims");
    private Vector4 guideMaterialDimsVector;
    private int tableModel = Int32.MinValue;

    [Space(10)]
    [Header("reference of billiards module")]
    [SerializeField] public BilliardsModule table;
    [SerializeField] public float tableParamPollingInterval = 3.0f;
    
    private int repositionCount;
    private bool[] repositioning;
    
    private int[] targetIndex;
    
    //[UdonSynced] [NonSerialized] public bool enabledSynced;
    [UdonSynced] [NonSerialized] public Vector3[] ballsPSynced;
    [UdonSynced] [NonSerialized] public int[] targetIndexSynced;
    [UdonSynced] [NonSerialized] public Vector2[] targetGuideSynced;
    [UdonSynced] [NonSerialized] public Vector2[] followGuideSynced;

    private const int TABLE_MIRROR_UNIT = 5;
    private /* const */ float TABLE_LONG_OFFSET = 2.064f; // - k_BALL_RADIUS; // 2.063f 僅かに行き過ぎ // 2.065f 僅かに足りない // 2.07f ちょっと足りない // 2.06f ちょっと行き過ぎ
    private /* const */ float TABLE_SHORT_OFFSET = 1.15f; // - k_BALL_RADIUS;
    private GameObject[] imageBallInMirror = new GameObject[TABLE_MIRROR_UNIT * TABLE_MIRROR_UNIT];
    private GameObject[] followGuidelineInMirror = new GameObject[TABLE_MIRROR_UNIT * TABLE_MIRROR_UNIT];
    private GameObject[] targetBallInMirror = new GameObject[TABLE_MIRROR_UNIT * TABLE_MIRROR_UNIT];
    private GameObject[] targetGuidelineInMirror = new GameObject[TABLE_MIRROR_UNIT * TABLE_MIRROR_UNIT];
    private Material[] followGuideDisplayMaterialInMirror;
    private Material[] targetGuideDisplayMaterialInMirror;
    //private Vector3[] ballsPSyncedInMirror;
    private GameObject[][] imageBallInMirrorMatrix = new GameObject[TABLE_MIRROR_UNIT][]{
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT]
    };
    private GameObject[][] followGuidelineInMirrorMatrix = new GameObject[TABLE_MIRROR_UNIT][]{
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT]
    };
    private GameObject[][] targetBallInMirrorMatrix = new GameObject[TABLE_MIRROR_UNIT][]{
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT]
    };
    private GameObject[][] targetGuidelineInMirrorMatrix = new GameObject[TABLE_MIRROR_UNIT][]{
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT],
        new GameObject[TABLE_MIRROR_UNIT]
    };
    private Vector3[][] mirrorTableCenterPositions = new Vector3[TABLE_MIRROR_UNIT][]{
        new Vector3[TABLE_MIRROR_UNIT],
        new Vector3[TABLE_MIRROR_UNIT],
        new Vector3[TABLE_MIRROR_UNIT],
        new Vector3[TABLE_MIRROR_UNIT],
        new Vector3[TABLE_MIRROR_UNIT]
    };
    float[][] mirrordPatternsX =
    {
        new float[2],
        new float[2]
    };
    float[][] mirrordPatternsZ =
    {
        new float[2],
        new float[2]
    };
    int[][] mirrordAngleFlips =
    {
        new int[2],
        new int[2]
    };
    float[][] mirrordAngleAdjusts =
    {
        new float[2],
        new float[2]
    };
 
    public override void OnDeserialization()
    {
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log("TKCH ImageBallManager::OnDeserialization() start");
#endif
        
        /*
        if (!enabledSynced)
        {
            this.gameObject.SetActive(false);
#if TKCH_DEBUG_IMAGE_BALLS
            table._Log("TKCH ImageBallManager::OnDeserialization() return");
#endif
            return;
        }

        this.gameObject.SetActive(true);
        */
        
        Array.Copy(targetIndexSynced, targetIndex, targetGuideSynced.Length);
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
        
        inMirrorPositionUpdate();
        
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log("TKCH ImageBallManager::OnDeserialization() end");
#endif
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
            syncValueUpdate();
            RequestSerialization();
        }
    }

    /*
    private void OnDisable()
    {
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log("TKCH ImageBallManager::OnDisable()");
#endif
        
        enabledSynced = false;
        RequestSerialization();
    }
    */

    private void OnEnable()
    {
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log("TKCH ImageBallManager::OnEnable()");
#endif
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log($"  table.transform.eulerAngles.y = {table.transform.eulerAngles.y}");
        table._Log($"  table.transform.localEulerAngles.y = {table.transform.localEulerAngles.y}");
#endif
        
        //enabledSynced = true;
        ballsPSynced = new Vector3[imageBalls.Length];
        targetGuideline = new GameObject[imageBalls.Length];
        followGuideline = new GameObject[imageBalls.Length];
        targetGuideDisplayMaterial = new Material[imageBalls.Length];
        followGuideDisplayMaterial = new Material[imageBalls.Length];
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
            // imageBalls[i].transform.localEulerAngles = Vector3.zero;
        }

        for (int i = 0; i < targetGuideline.Length; i++)
        {
            // targetGuideline[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material.SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
            targetGuideDisplayMaterial[i] =targetGuideline[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material;
            targetGuideDisplayMaterial[i].SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
        }
        for (int i = 0; i < followGuideline.Length; i++)
        {
            // followGuideline[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material.SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
            followGuideDisplayMaterial[i] = followGuideline[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material;
            followGuideDisplayMaterial[i].SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
        }

        if (!ReferenceEquals(null, imageBallInMirrorParent))
        {
            Quaternion rotation = imageBallInMirrorParent.transform.rotation;
            imageBallInMirrorParent.transform.eulerAngles = new Vector3(
                rotation.eulerAngles.x,
                table.transform.eulerAngles.y,
                rotation.eulerAngles.z
            );

            initializeMirrorTable();
            for (int i = 0; i < imageBallInMirror.Length; i++)
            {
                imageBallInMirror[i].SetActive(true);
            }
            imageBallInMirror[(imageBallInMirror.Length / 2)].SetActive(false); // dup orig
        }
        
        _Init();
        SendCustomEventDelayedSeconds(nameof(_DelayInit), 1.0f);
    }

    public void _Init()
    {
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log("TKCH ImageBallManager::_Init() start");
#endif
        repositioning = new bool[table.balls.Length];
        
        repositionCount = 0;
        Array.Clear(repositioning, 0, repositioning.Length);

        inMirrorPositionUpdate();
        
#if TKCH_DEBUG_IMAGE_BALLS
        table.SetProgramVariable("callbacks", this);
#endif
    }

    public void _DelayInit()
    {
        TableParamUpdate();
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log($"  transformSurface.position.y = {transformSurface.position.y}");
#endif
        for (int i = 0; i < imageBalls.Length; i++)
        {
            MeshRenderer meshRenderer = imageBalls[i].GetComponent<MeshRenderer>();
            MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
            // materialPropertyBlock.SetFloat(_Floor, transformSurface.position.y - k_BALL_RADIUS);
            materialPropertyBlock.SetFloat("_Floor", transformSurface.position.y - k_BALL_RADIUS);
            meshRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }

#if TKCH_DEBUG_IMAGE_BALLS
    public void _OnLobbyOpened()
    {
        for (int i = 0; i < imageBalls.Length; i++)
        {
            MeshRenderer meshRenderer = imageBalls[i].GetComponent<MeshRenderer>();
            Material ballShadow = meshRenderer.sharedMaterials[1];
            table._Log($"  {ballShadow.name} _Floor = {ballShadow.GetFloat("_Floor")}");
            // table._Log($"  {ballShadow.name} _Floor = {ballShadow.GetFloat(_Floor)}");
            // ballShadow = meshRenderer.materials[1];
            // table._Log($"  {ballShadow.name} _Floor = {ballShadow.GetFloat(_Floor)}");
            //
            // Material ballMarker = meshRenderer.sharedMaterial;
            // table._Log($"  {ballMarker.name} _FresnelPower = {ballMarker.GetFloat(_FresnelPower)}");
            // ballMarker = meshRenderer.material;
            // table._Log($"  {ballMarker.name} _FresnelPower = {ballMarker.GetFloat(_FresnelPower)}");
            
            table._Log($"  meshRenderer.HasPropertyBlock() = {meshRenderer.HasPropertyBlock()}");
            // table._Log($"  followGuideDisplay[{i}].HasPropertyBlock() = {followGuideDisplay[i].HasPropertyBlock()}");
            // table._Log($"  targetGuideDisplay[{i}].HasPropertyBlock() = {targetGuideDisplay[i].HasPropertyBlock()}");
        }
        // table._Log($"  guideMaterialPropertyBlock _Dims.x = {guideMaterialPropertyBlock.GetVector(_Dims).x}");
        table._Log($"  _Dims = {guideMaterialDimsVector.x}, {guideMaterialDimsVector.y}");
        // table._Log($"  guideDisplayMaterial _Dims = {guideDisplayMaterial.GetVector(_Dims).x}, {guideDisplayMaterial.GetVector(_Dims).y}");
    }
#endif
    
/*
    public void _OnLobbyClosed()
    {
    }

    public void _OnGameStarted()
    {
       // repositionCount = 0;
       // Array.Clear(repositioning, 0, repositioning.Length);
       //
       // inMirrorPositionUpdate();
    }
*/

    public void TableParamUpdate()
    {
#if TKCH_DEBUG_IMAGE_BALLS
        table._Log("TKCH ImageBallManager::TableParamUpdate() start");
#endif
        int tableModelLocal = (int)table.GetProgramVariable("tableModelLocal");
        if (tableModel == tableModelLocal)
        {
            if (0 < tableParamPollingInterval)
            {
                SendCustomEventDelayedSeconds(nameof(TableParamUpdate), tableParamPollingInterval);
            }
            return;
        }

#if TKCH_DEBUG_IMAGE_BALLS
        table._Log($"TKCH  tableModel changed {tableModel} -> {tableModelLocal}");
#endif
        tableModel = tableModelLocal;
        for (int i = 0; i < imageBalls.Length; i++)
        {
            hideGuideline(i);
        }
        
        UdonSharpBehaviour physicsManager = table.currentPhysicsManager;
        Vector3 k_pR = (Vector3)physicsManager.GetProgramVariable("k_pR");
        Vector3 k_pO = (Vector3)physicsManager.GetProgramVariable("k_pO");
        maxX = k_pR.x;
        maxZ = k_pO.z;

        Vector3 k_vE = (Vector3)table.GetProgramVariable("k_vE");
#if TKCH_DEBUG_IMAGE_BALLS
        // pp103 k_vE = (1.09, 0.00, 0.63)
        // VRCSA_classic k_vE = (1.09, 0.00, 0.63)
        // VRCSA_7ft     k_vE = (1.01, 0.00, 0.52)
        // Guideline      1.23, 0.764    x = 0.14, z = 0.134
        // Guideline_bank 1.017, 0.561   x = -0.073, z = -0.069
        // table._Log($"TKCH  k_vE = {k_vE}");
#endif
        guideMaterialDimsVector = new Vector4(k_vE.x, k_vE.z, 0, 0);
#if TKCH_DEBUG_IMAGE_BALLS
        // table._Log($"TKCH  guideMaterialDimsVector = {guideMaterialDimsVector}");
#endif

        transformSurface = (Transform)physicsManager.GetProgramVariable("transform_Surface");
        if (ReferenceEquals(null, transformSurface))
        {
            transformSurface = (Transform)physicsManager.GetProgramVariable("table_Surface");

            k_BALL_RADIUS = (float)physicsManager.GetProgramVariable("k_BALL_RADIUS");
            maxX = k_pR.x - k_BALL_RADIUS;
            maxZ = k_pO.z - k_BALL_RADIUS;

            float k_BALL_DIAMETRE = (float)physicsManager.GetProgramVariable("k_BALL_DIAMETRE");
            float newscale = k_BALL_DIAMETRE / ballMeshDiameter;
            Vector3 newBallSize = Vector3.one * newscale;
            for (int i = 0; i < imageBalls.Length; i++)
            {
                imageBalls[i].transform.localScale = newBallSize;
                
                Transform imageBallMarker = imageBalls[i].transform.Find("image_ball_marker");
                imageBallMarker.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                imageBallMarker.localPosition = new Vector3(0, 0.0329f, 0);
            }

#if TKCH_DEBUG_IMAGE_BALLS
            table._Log($"TKCH  k_vE = {k_vE}, k_BALL_RADIUS = {k_BALL_RADIUS}"); // 0.028575
#endif

            if (!ReferenceEquals(null, imageBallInMirrorParent))
            {
                TABLE_LONG_OFFSET = ((k_pR.x * 2) - k_BALL_RADIUS);
                TABLE_SHORT_OFFSET = ((k_pO.z * 2) - k_BALL_RADIUS);
#if TKCH_DEBUG_IMAGE_BALLS
                table._Log($"TKCH  TABLE_LONG_OFFSET = {TABLE_LONG_OFFSET}");
                table._Log($"TKCH  TABLE_SHORT_OFFSET = {TABLE_SHORT_OFFSET}");
#endif
                initializeMirrorTableCenterPositions();
                
#if TKCH_DEBUG_IMAGE_BALLS
                for (int i = 0; i < imageBallInMirror.Length; i++)
                {
                    imageBallInMirror[i].transform.localScale = newBallSize;
                    targetBallInMirror[i].transform.localScale = newBallSize;
                }
#endif
                guideMaterialDimsVector = new Vector4(k_pR.x - k_BALL_RADIUS, k_pO.z - k_BALL_RADIUS, 0, 0);

                for (int i = 0; i < imageBallInMirror.Length; i++)
                {
                    followGuideDisplayMaterialInMirror[i].SetVector("_Dims", guideMaterialDimsVector);
                    targetGuideDisplayMaterialInMirror[i].SetVector("_Dims", guideMaterialDimsVector);
                }
            }
        }
        else
        {
            if (!ReferenceEquals(null, imageBallInMirrorParent))
            {
                guideMaterialDimsVector = new Vector4(k_vE.x - 0.073f, k_vE.z - 0.069f, 0, 0);
#if TKCH_DEBUG_IMAGE_BALLS
                table._Log($"TKCH  guideMaterialDimsVector = {guideMaterialDimsVector}");
#endif
            }
        }
        
        for (int i = 0; i < targetGuideline.Length; i++)
        {
            targetGuideDisplayMaterial[i].SetVector("_Dims", guideMaterialDimsVector);
            // targetGuideDisplayMaterial[i].SetVector(_Dims, guideMaterialDimsVector);
        }
        for (int i = 0; i < followGuideline.Length; i++)
        {
            followGuideDisplayMaterial[i].SetVector("_Dims", guideMaterialDimsVector);
            // followGuideDisplayMaterial[i].SetVector(_Dims, guideMaterialDimsVector);
        }

        if (!ReferenceEquals(null, imageBallInMirrorParent))
        {
            guideScaleBase = 0.12f; // carom 0.2f bank 0.12f, normal 0.04f
        }

        if (0 < tableParamPollingInterval)
        {
            SendCustomEventDelayedSeconds(nameof(TableParamUpdate), tableParamPollingInterval);
        }
    }

    private void Update()
    {
        this._Tick();
    }

    public void _Tick()
    {
        if (repositionCount == 0) return;

        // TableParamUpdate();
        
        for (int i = 0; i < repositioning.Length; i++)
        {
            if (!repositioning[i]) continue;

            GameObject imageBall = imageBalls[i];
           
            Transform pickupTransform = imageBall.transform.GetChild(0);
            
            Vector3 boundedLocation = table.transform.InverseTransformPoint(pickupTransform.position);
            boundedLocation.x = Mathf.Clamp(boundedLocation.x, -maxX, maxX);
            boundedLocation.z = Mathf.Clamp(boundedLocation.z, -maxZ, maxZ);
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

            float targetGuidelineAdjust = 0; // 90 + transform.localEulerAngles.y + table.transform.localEulerAngles.y;
            
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
                    // targetGuideline[i].transform.localEulerAngles = new Vector3(0.0f, g2tDeg + targetGuidelineAdjust, 0.0f);
                    targetGuideline[i].transform.eulerAngles = new Vector3(0.0f, g2tDeg + targetGuidelineAdjust, 0.0f);
#if TKCH_DEBUG_IMAGE_BALLS
                    table._Log($"  targetGuideline[{i}].transform.eulerAngles.y = {targetGuideline[i].transform.eulerAngles.y}");
                    table._Log($"  targetGuideline[{i}].transform.localEulerAngles.y = {targetGuideline[i].transform.localEulerAngles.y}");
#endif
                    
                    float ct2gtDiff = g2tDeg - c2tDeg;

                    float followGuidelineAdjust2 = targetGuidelineAdjust - 90;
                    if ((-180 <= ct2gtDiff && ct2gtDiff < 0) || 180 <= ct2gtDiff)
                    {
                        followGuidelineAdjust2 += 180;
                    }
                    // followGuideline[i].transform.localEulerAngles = new Vector3(0.0f, g2tDeg + followGuidelineAdjust2, 0.0f);
                    followGuideline[i].transform.eulerAngles = new Vector3(0.0f, g2tDeg + followGuidelineAdjust2, 0.0f);
                    
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
                        followScale = guideScaleBase * (ratio);
                        targetScale = guideScaleBase * (1 - ratio);
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
                pos.y = transformSurface.position.y; // 0.8606015f;

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
            
            inMirrorPositionUpdate();
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

        syncValueUpdate();
        
        this.RequestSerialization();
    }


    public void hideGuideline(int idx)
    {
        targetGuideline[idx].SetActive(false);
        followGuideline[idx].SetActive(false);

        if (!ReferenceEquals(null, imageBallInMirrorParent))
        {
            for (int i = 0; i < imageBallInMirror.Length; i++)
            {
                targetGuidelineInMirror[i].SetActive(false);
                followGuidelineInMirror[i].SetActive(false);
            }
        }
    }

    public void syncValueUpdate()
    {
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

        inMirrorPositionUpdate();
    }

    private void initializeMirrorTable()
    {
        initializeMirrorTableCenterPositions();

        //ballsPSyncedInMirror = new Vector3[imageBallInMirror.Length];
        followGuideDisplayMaterialInMirror = new Material[imageBallInMirror.Length];
        targetGuideDisplayMaterialInMirror = new Material[imageBallInMirror.Length];
        
        for (int i = 0; i < imageBallInMirror.Length; i++)
        {
            imageBallInMirror[i] = imageBallInMirrorParent.transform.Find($"ImageBall_{i}").gameObject;
            followGuidelineInMirror[i] = imageBallInMirror[i].transform.Find("follow_guide").gameObject;
            targetGuidelineInMirror[i] = imageBallInMirror[i].transform.Find("target_guide").gameObject;
            followGuideDisplayMaterialInMirror[i] = followGuidelineInMirror[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material;
            targetGuideDisplayMaterialInMirror[i] = targetGuidelineInMirror[i].transform.Find("guide_display").GetComponent<MeshRenderer>().material;
            followGuideDisplayMaterialInMirror[i].SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
            targetGuideDisplayMaterialInMirror[i].SetMatrix("_BaseTransform", table.transform.worldToLocalMatrix);
            targetBallInMirror[i] = imageBallInMirrorParent.transform.Find($"Cylinder_{i}").gameObject; // ないのでとりあえず
        }

        for (int x = 0; x < imageBallInMirrorMatrix.Length; x++)
        {
            for (int z = 0; z < imageBallInMirrorMatrix[x].Length; z++)
            {
                imageBallInMirrorMatrix[x][z] = imageBallInMirror[(x * TABLE_MIRROR_UNIT) + z];
                followGuidelineInMirrorMatrix[x][z] = followGuidelineInMirror[(x * TABLE_MIRROR_UNIT) + z];
                targetBallInMirrorMatrix[x][z] = targetBallInMirror[(x * TABLE_MIRROR_UNIT) + z];
                targetGuidelineInMirrorMatrix[x][z] = targetGuidelineInMirror[(x * TABLE_MIRROR_UNIT) + z];
            }
        }
    }
    
    private void initializeMirrorTableCenterPositions()
    {
        int negativeAdjustOffset = (TABLE_MIRROR_UNIT / 2); // 5 -> -2 center set to 0,0
        for (int x = 0; x < mirrorTableCenterPositions.Length; x++)
        {
            int ox = x - negativeAdjustOffset;
            for (int z = 0; z < mirrorTableCenterPositions[x].Length; z++)
            {
                int oz = z - negativeAdjustOffset;
                mirrorTableCenterPositions[x][z] = new Vector3(
                    (TABLE_LONG_OFFSET - k_BALL_RADIUS) * ox,
                    0,
                    (TABLE_SHORT_OFFSET - k_BALL_RADIUS) * oz
                );
            }
        }
    }

    private void inMirrorPositionUpdate()
    {
#if TKCH_DEBUG_IMAGE_BALLS
        // table._Log("TKCH ImageBallManager::inMirrorPositionUpdate() start");
#endif
        if (ReferenceEquals(null, imageBallInMirrorParent))
        {
            return;
        }

        Vector3 relativePosition = table.transform.InverseTransformPoint(imageBalls[0].transform.position);
        inMirrorPositionUpdate(relativePosition, imageBallInMirrorMatrix);
        inMirrorPositionUpdateGuidelines(imageBallInMirror, followGuideline[0], followGuidelineInMirror, followGuidelineInMirrorMatrix);
        //inMirrorUpdateGuidelines(imageBalls[0].transform.localPosition, followGuideline[0], followGuidelineInMirror, followGuidelineInMirrorMatrix);
        bool targetEnable = 0 <= targetIndex[0];
        if (targetEnable)
        {
            inMirrorPositionUpdate(
                table.balls[targetIndex[0]].transform.localPosition, targetBallInMirrorMatrix);
        }
        for (int i = 0; i < targetBallInMirror.Length; i++)
        {
            //targetBallInMirror[i].SetActive(targetEnable);
        }
#if TKCH_DEBUG_IMAGE_BALLS
        // table._Log($"TKCH  targetBallInMirror is null ? {ReferenceEquals(null, targetBallInMirror)}");
        // table._Log($"TKCH  targetBallInMirror.Length = {targetBallInMirror.Length}");
        // table._Log($"TKCH  targetBallInMirror[{(targetBallInMirror.Length / 2)}] is null ? {ReferenceEquals(null, targetBallInMirror[(targetBallInMirror.Length / 2)])}");
#endif
        targetBallInMirror[(targetBallInMirror.Length / 2)].SetActive(false); // dup orig
        inMirrorPositionUpdateGuidelines(targetBallInMirror, targetGuideline[0], targetGuidelineInMirror, targetGuidelineInMirrorMatrix);
        //inMirrorUpdateGuidelines((targetEnable ? table.balls[targetIndex[0]].transform.localPosition : Vector3.negativeInfinity), targetGuideline[0], targetGuidelineInMirror, targetGuidelineInMirrorMatrix);
    }
    
    private void inMirrorUpdateGuidelines(
        Vector3 localPosition, //GameObject[] ballInMirror, 
        GameObject guideline, 
        GameObject[] guidelineInMirror, 
        GameObject[][] guidelineInMirrorMatrix)
    {
        if (Vector3.negativeInfinity == localPosition)
        {
            return;
        }
        
        bool active = guideline.activeSelf;
        //Vector3 position = guideline.transform.localPosition;
        Vector3 localScale = guideline.transform.localScale;
        Vector3 angle = guideline.transform.localEulerAngles;
        
        for (int x = 0; x <= 1; x++)
        {
            int sx = x == 0 ? 1 : -1;
            for (int z = 0; z <= 1; z++)
            {
                int sz = z == 0 ? 1 : -1;
                mirrordPatternsX[x][z] = localPosition.x * sx;
                mirrordPatternsZ[x][z] = localPosition.z * sz;
                mirrordAngleFlips[x][z] = sx * sz;
                mirrordAngleAdjusts[x][z] = z == 0 ? 0 : 180;
            }
        }

        for (int x = 0; x < guidelineInMirrorMatrix.Length; x++)
        {
            for (int z = 0; z < guidelineInMirrorMatrix[x].Length; z++)
            {
                guidelineInMirrorMatrix[x][z].SetActive(active);
                if (active)
                {
                    int x2 = x % 2;
                    int z2 = z % 2;
                    guidelineInMirrorMatrix[x][z].transform.localPosition = new Vector3(
                        mirrorTableCenterPositions[x][z].x + mirrordPatternsX[x2][z2],
                        0,
                        mirrorTableCenterPositions[x][z].z + mirrordPatternsZ[x2][z2]
                    );
                    guidelineInMirrorMatrix[x][z].transform.localEulerAngles = new Vector3(
                        0,
                        (angle.y + mirrordAngleAdjusts[x2][z2]) * mirrordAngleFlips[x2][z2],
                        0
                    );
                    //guidelineInMirrorMatrix[x][z].transform.localScale = scale;
                    guidelineInMirrorMatrix[x][z].transform.localScale = localScale;
                }
            }
        }
    }

    private void inMirrorPositionUpdateGuidelines(
        GameObject[] ballInMirror, 
        GameObject guideline, 
        GameObject[] guidelineInMirror, 
        GameObject[][] guidelineInMirrorMatrix)
    {
        bool active = guideline.activeSelf;
        //Vector3 position = guideline.transform.localPosition;
        Vector3 scale = guideline.transform.localScale;
        //scale.x *= 3;
        Quaternion rotation = guideline.transform.rotation * Quaternion.Inverse(table.transform.rotation);
        Vector3 angle = rotation.eulerAngles;
        for (int i = 0; i < guidelineInMirror.Length; i++)
        {
            guidelineInMirror[i].SetActive(active);
            if (active)
            {
                //guidelineInMirror[i].transform.localPosition = localPosition;
                guidelineInMirror[i].transform.position = ballInMirror[i].transform.position;
                guidelineInMirror[i].transform.localScale = scale;
            }
        }
        
        for (int x = 0; x <= 1; x++)
        {
            int sx = x == 0 ? 1 : -1;
            for (int z = 0; z <= 1; z++)
            {
                int sz = z == 0 ? 1 : -1;
                mirrordAngleFlips[x][z] = sx * sz;
                mirrordAngleAdjusts[x][z] = z == 0 ? 0 : 180;
            }
        }
        for (int x = 0; x < guidelineInMirrorMatrix.Length; x++)
        {
            for (int z = 0; z < guidelineInMirrorMatrix[x].Length; z++)
            {
                int x2 = x % 2;
                int z2 = z % 2;
                guidelineInMirrorMatrix[x][z].transform.localEulerAngles = new Vector3(
                    0,
                    (angle.y - table.transform.localEulerAngles.y + mirrordAngleAdjusts[x2][z2]) * mirrordAngleFlips[x2][z2],
                    0
                );
            }
        }
    }

    private void inMirrorPositionUpdate(Vector3 localPosition, GameObject[][] inMirrorMatrix)
    {
        for (int x = 0; x <= 1; x++)
        {
            int sx = x == 0 ? 1 : -1;
            for (int z = 0; z <= 1; z++)
            {
                int sz = z == 0 ? 1 : -1;
                mirrordPatternsX[x][z] = localPosition.x * sx;
                mirrordPatternsZ[x][z] = localPosition.z * sz;
            }
        }
        
        for (int x = 0; x < mirrorTableCenterPositions.Length; x++)
        {
            for (int z = 0; z < mirrorTableCenterPositions[x].Length; z++)
            {
                int x2 = x % 2;
                int z2 = z % 2;
                inMirrorMatrix[x][z].transform.localPosition = new Vector3(
                    mirrorTableCenterPositions[x][z].x + mirrordPatternsX[x2][z2],
                   0,
                    mirrorTableCenterPositions[x][z].z + mirrordPatternsZ[x2][z2]
                );
            }
        }
    }
}