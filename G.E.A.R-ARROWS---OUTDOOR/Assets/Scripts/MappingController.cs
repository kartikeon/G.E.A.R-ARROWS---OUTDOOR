 using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Android;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;


public class MappingController : MonoBehaviour
{
    
    #region INSPECTOR_VARIABLES   
    public  GameObject spherePrefab, forwardPrefab, ChooseArrows, diamond, Arrow, Arrow_Bar, RedBlack_Arrow, Bat, BlackPanther, Deadpool, Ironman, Joker, Spidey; 
    public GameObject linktransform;
    public AudioClip a1,a2;
    public AudioSource audioSource;
    public MessageBehavior messageBehavior;
    public TextMeshProUGUI driftText, informationText, distanceText, stxt, Dirtxt;
    [Tooltip("Divides the screen along the height and width by that much times for array raycasting")]
    public int screenSplitCount = 5;
    // [Tooltip("Enable this to instantiate objects along distance, only initial anchor will be present; Disable to use array raycasting for finding feature points to anchor objects along the way")]
    [Tooltip("Range in meters for array raycasting")]
    public float raycastRange = 1.5f;
    public float gpsTolerance = 0.00005f;
    public Toggle toggleInstantiationTypeButton;
    public Toggle toggleARFoundationButton;
    #endregion

    #region PRIVATE_VARIABLES
    ARSessionOrigin arSessionOrigin;
    ARRaycastManager arRaycastManager;
    ARPlaneManager arPlaneManager;
    ARPointCloudManager arPointCloudManager;


    List<ARRaycastHit> hits = new List<ARRaycastHit>();
    List<ARRaycastHit> wayHits = new List<ARRaycastHit>();
    List<GameObject> instantiatedObjects = new List<GameObject>();

    GameObject origin;
    bool isPlaying;
    bool isOriginFound;
    bool isVisible;
    bool shouldInstantiateWithDistance = false;
    bool isARFoundationEnabled;
    bool isGPSEnabled;
    bool isAtOriginPoint;

     Vector3 initialPos, currentPos, previousPos;

    LocationService locationService;
    Coordinates initialCoordinates;
    Coordinates finalCoordinates;

     public float lat1 ,long1 ,lat2 ,long2, distance1;
     public string label;

     public LinkTransform link;
    
     public int y,k;

    List<Vector2> screenPointsForRaycasting = new List<Vector2>();
    #endregion
   
    void Awake()
    {
        ValueY();
        print("SYS ValueY :" + y);

        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            Permission.RequestUserPermission(Permission.FineLocation);
            
            
    }

    public void ValueY()
    {
        linktransform = GameObject.FindGameObjectsWithTag("LinkTransform")[0] as GameObject;
        link = linktransform.GetComponent<LinkTransform>();
        y = link.A;
        k = link.Z;

         switch (k)
         {
             case 0:
             ChooseArrows = RedBlack_Arrow;
             break;

             case 1:
             ChooseArrows = Arrow;
             break;

             case 2:
             ChooseArrows = Arrow_Bar;
             break;

             case 3:
             ChooseArrows = forwardPrefab;
             break;

             case 4:
             ChooseArrows = Spidey;
             break;

             case 5:
             ChooseArrows = Ironman;
             break;

             case 6:
             ChooseArrows = Bat;
             break;

             case 7:
             ChooseArrows = Deadpool;
             break;

             case 8:
             ChooseArrows = BlackPanther;
             break;

             case 9:
             ChooseArrows = Joker;
             break;

             default:
             break;
         }
    }

        void Start()
    {
        

        isPlaying = false;
        isOriginFound = false;
        isVisible = true;
        isGPSEnabled = false;
        isAtOriginPoint = false;

        arRaycastManager = FindObjectOfType<ARRaycastManager>();
        arPlaneManager = FindObjectOfType<ARPlaneManager>();
        arPointCloudManager = FindObjectOfType<ARPointCloudManager>();

         GetScreenPoints();

         CoordinatesManager();
         
         //RouteManager();

        initialCoordinates.gpsLat = lat1;   
        initialCoordinates.gpsLong = long1;                     
        finalCoordinates.gpsLat = lat2;      
        finalCoordinates.gpsLong =long2;
        
        

        StartCoroutine(GenerateRoutePrefabs());

    }

    #region PUBLIC_METHODS

    public void CreateSpheresOnTheGo()
    {
        shouldInstantiateWithDistance = toggleInstantiationTypeButton.isOn;
        toggleInstantiationTypeButton.interactable = false;

        isARFoundationEnabled = toggleARFoundationButton.isOn;
        toggleARFoundationButton.interactable = false;

        isPlaying = true;
        if (shouldInstantiateWithDistance)
            StartCoroutine(InitiateMultiWorldPlacement());
        else
            StartCoroutine(InstantiateAtNearestFeaturePoint());
    }

    public void StopSpheres()
    {
        toggleInstantiationTypeButton.interactable = true;
        toggleARFoundationButton.interactable = true;

        isPlaying = false;
        isOriginFound = false;
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;

        foreach (GameObject go in instantiatedObjects)
        {
            go.transform.GetComponent<Renderer>().enabled = isVisible;
        }

    }

    public void StartRouting()
    {
        isAtOriginPoint = true;
    }

    void GetScreenPoints()
    {
        float incrementalWidth = Screen.width / screenSplitCount;
        float incrementalHeight = Screen.height / screenSplitCount;

        screenPointsForRaycasting.Clear();
        for (Vector2 currentPoint = Vector2.zero; currentPoint.y <= Screen.height; currentPoint.y += incrementalHeight)
        {
            for (currentPoint.x = 0; currentPoint.x <= Screen.width; currentPoint.x += incrementalWidth)
            {
                screenPointsForRaycasting.Add(currentPoint);
                Debug.Log("POINTS : " + currentPoint);
            }
        }

    }

    public void ClearAllSpheres()
    {
        foreach (GameObject go in instantiatedObjects)
            Destroy(go.gameObject);

        instantiatedObjects.Clear();
    }

   
  


    public void BackButton()
    {  

        SceneManager.MoveGameObjectToScene(linktransform, SceneManager.GetActiveScene());
        SceneManager.LoadScene(0);

    }


    public void SetOrigin()
    {
        if (isARFoundationEnabled)
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (arRaycastManager.Raycast(touch.position, hits, TrackableType.FeaturePoint))
                {
                    origin = Instantiate(spherePrefab, hits[0].pose.position, Quaternion.identity);
                    instantiatedObjects.Add(origin);
                    previousPos = Camera.main.transform.position;
                    isOriginFound = true;
                }
            }
        }
        else
        {
            origin = Instantiate(spherePrefab, Camera.main.transform.position, Quaternion.identity);
            instantiatedObjects.Add(origin);
            previousPos = Camera.main.transform.position;
            isOriginFound = true;
        }
    }




    public Vector3 GeneratePathInDirection(Vector3 currentPosition,int distanceInMeters,DIRECTION turnDirection)
    {
        currentPos = currentPosition;
        float angle;

        switch (turnDirection) 
        {
           case DIRECTION.FORWARD:
                angle = 0;
                break;
                
            case DIRECTION.RIGHT:     
                angle = 90;
                break;

           case DIRECTION.LEFT:     
                angle = -90;
                break;

           case DIRECTION.RIGHT45:
                angle = 45;
                break;

            case DIRECTION.LEFT45:
                angle = -45;
                break;

            case DIRECTION.BACKWARD:
                angle = 180;
                break;

               default:
                angle = 0;
                break;
        }

        for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }

        for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }

       /* for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(Arrow, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }

        for (int meterCount = 0; meterCount < distanceInMeters; meterCount++)
        {
            GameObject go = Instantiate(Arrow_Bar, currentPos, Camera.main.transform.rotation);
            Camera.main.transform.DetachChildren();
            go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + angle, go.transform.rotation.eulerAngles.z)));
            instantiatedObjects.Add(go);
            currentPos += go.transform.forward;

        }*/

       

        return currentPos;
    }


    #endregion



    #region COROUTINES
    public IEnumerator InstantiateAtNearestFeaturePoint()
    {

        yield return new WaitUntil(() => isPlaying);

        while (!isOriginFound)
        {
            SetOrigin();
            yield return new WaitForEndOfFrame();
        }

        while (isPlaying)
        {
            currentPos = Camera.main.transform.position;
            //  Vector3 instantiatePos = new Vector3(currentPos.x, .5f, currentPos.z);
            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                foreach (Vector2 screenPoint in screenPointsForRaycasting)
                {
                    wayHits.Clear();
                    if (arRaycastManager.Raycast(screenPoint, wayHits, TrackableType.FeaturePoint))
                    {
                        if (Vector3.Distance(currentPos, wayHits[0].pose.position) < raycastRange)
                        {
                            GameObject go = Instantiate(ChooseArrows, wayHits[0].pose.position, wayHits[0].pose.rotation);
                            instantiatedObjects.Add(go);
                            previousPos = currentPos;
                            break;

                        }

                         if (Vector3.Distance(currentPos, wayHits[0].pose.position) < raycastRange)
                        {
                            GameObject go = Instantiate(diamond, wayHits[0].pose.position, wayHits[0].pose.rotation);
                            instantiatedObjects.Add(go);
                            previousPos = currentPos;
                            break;

                        }
                        
                    }
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator InitiateMultiWorldPlacement()
    {
        yield return new WaitUntil(() => isPlaying);

        while (!isOriginFound)
        {
            SetOrigin();
            yield return new WaitForEndOfFrame();
        }

        while (isPlaying)
        {
            currentPos = Camera.main.transform.position;
            //  Vector3 instantiatePos = new Vector3(currentPos.x, .5f, currentPos.z);
            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(ChooseArrows, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }

            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(diamond, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }

            /*if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(Arrow, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }

            if (Vector3.Distance(currentPos, previousPos) > 1)
            {
                GameObject go = Instantiate(Arrow_Bar, Camera.main.transform);
                Camera.main.transform.DetachChildren();
                go.transform.SetPositionAndRotation(go.transform.position, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));

                instantiatedObjects.Add(go);
                previousPos = currentPos;
            }*/

            
            yield return new WaitForEndOfFrame();
        }
    }
    







   public int dir;
    public void RouteManager()
    {
         
           // ValueY();
            dir = y;
            print("SYS Dir: " + dir);

                switch (dir)
                {

                    case 12:
                    Direction12();
                    print("SYS dir 12: " + dir);
                    break;

                    case 21:
                    Direction21();
                    print("SYS dir 21: " + dir);
                    break;

                    case 13:
                    Direction13();
                    print("SYS dir 13: " + dir);
                    break;

                    case 31:
                    Direction31();
                    print("SYS dir 31: " + dir);
                    break; 
                    
                    case 14:
                    Direction14();
                    print("SYS dir 14: " + dir);
                    break;

                    case 41:
                    Direction41();
                    print("SYS dir 41: " + dir);
                    break;

                    case 15:
                    Direction15();
                    print("SYS dir 15: " + dir);
                    break;

                    case 51:
                    Direction51();
                    print("SYS dir 51: " + dir);
                    break;

                    case 16:
                    Direction16();
                    print("SYS dir 16: " + dir);
                    break; 
                    
                    case 61:
                    Direction61();
                    print("SYS dir 61: " + dir);
                    break;

                    case 23:
                    Direction23();
                    print("SYS dir 23: " + dir);
                    break;

                    case 32:
                    Direction32();
                    print("SYS dir 32: " + dir);
                    break;

                     case 24:
                    Direction24();
                    print("SYS dir 24: " + dir);
                    break;

                    case 42:
                    Direction42();
                    print("SYS dir 42: " + dir);
                    break; 
                    
                    case 25:
                    Direction25();
                    print("SYS dir 25: " + dir);
                    break;

                    case 52:
                    Direction52();
                    print("SYS dir 52: " + dir);
                    break;

                    case 26:
                    Direction26();
                    print("SYS dir 26: " + dir);
                    break;

                    case 62:
                    Direction62();
                    print("SYS dir 62: " + dir);
                    break;

                    case 34:
                    Direction34();
                    print("SYS dir 34: " + dir);
                    break; 
                    
                    case 43:
                    Direction43();
                    print("SYS dir 43: " + dir);
                    break;

                    case 35:
                    Direction35();
                    print("SYS dir 35: " + dir);
                    break;

                    case 53:
                    Direction53();
                    print("SYS dir 53: " + dir);
                    break;

                     case 36:
                    Direction36();
                    print("SYS dir 36: " + dir);
                    break;

                    case 63:
                    Direction63();
                    print("SYS dir 63: " + dir);
                    break; 
                    
                    case 45:
                    Direction45();
                    print("SYS dir 45: " + dir);
                    break;

                    case 54:
                    Direction54();
                    print("SYS dir 21: " + dir);
                    break;

                    case 46:
                    Direction46();
                    print("SYS dir 46: " + dir);
                    break;

                    case 64:
                    Direction64();
                    print("SYS dir 12: " + dir);
                    break;

                    case 56:
                    Direction56();
                    print("SYS dir 56: " + dir);
                    break; 
                    
                    case 65:
                    Direction65();
                    print("SYS dir 65: " + dir);
                    break;

                    default:
                    break;

                }

    }

    public void CoordinatesManager()
    {
        int gps;
         //ValueY();
         gps = y;
         print("SYS gps: " + gps);

         switch (gps)
         {
             case 12:

             lat1 = 13.05402f;    
             long1 = 80.22703f;                         
             lat2 = 13.05420f;      
             long2 = 80.22676f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 21:
             lat1 = 13.05420f;      
             long1 = 80.22676f;
             lat2 = 13.05402f;    
             long2 = 80.22703f;                         
             
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 13:

             lat1 = 13.05402f;    
             long1 = 80.22703f;                         
             lat2 = 13.05460f;      
             long2 = 80.22657f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 31:
              lat1 = 13.05460f;      
             long1 = 80.22657f;                        
             lat2 = 13.05402f;    
             long2 = 80.22703f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

            case 14:

             lat1 = 13.05402f;    
             long1 = 80.22703f;                         
             lat2 = 13.05470f;      
             long2 = 80.22769f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 41:
             lat1 = 13.05470f;      
             long1 = 80.22769f;                         
             lat2 = 13.05402f;    
             long2 = 80.22703f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 15:

             lat1 = 13.05402f;    
             long1 = 80.22703f;                         
             lat2 = 13.05553f;      
             long2 = 80.22640f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 51:
             lat1 = 13.05553f;      
             long1 = 80.22640f;                         
             lat2 = 13.05402f;    
             long2 = 80.22703f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
             
             case 16:

             lat1 = 13.05402f;    
             long1 = 80.22703f;                         
             lat2 = 13.05584f;      
             long2 = 80.22663f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 61:
             lat1 = 13.05584f;      
             long1 = 80.22663f;                         
             lat2 = 13.05402f;    
             long2 = 80.22703f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

            case 23:

             lat1 = 13.05420f;      
             long1 = 80.22676f;                         
             lat2 = 13.05460f;      
             long2 = 80.22657f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 32:
             lat1 = 13.05460f;      
             long1 = 80.22657f;                       
              lat2 = 13.05420f;      
             long2 = 80.22676f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 24:

             lat1 = 13.05420f;      
             long1 = 80.22676f;                         
            lat2 = 13.05470f;      
             long2 = 80.22769f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 42:
             lat1 = 13.05470f;      
             long1 = 80.22769f;                         
              lat2 = 13.05420f;      
             long2 = 80.22676f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

            case 25:

             lat1 = 13.05420f;      
             long1 = 80.22676f;                     
            lat2 = 13.05553f;      
             long2 = 80.22640f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 52:
             lat1 = 13.05553f;      
             long1 = 80.22640f;                       
              lat2 = 13.05420f;      
             long2 = 80.22676f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 26:

             lat1 = 13.05420f;      
             long1 = 80.22676f;                         
             lat2 = 13.05584f;      
             long2 = 80.22663f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 62:
             lat1 = 13.05584f;      
             long1 = 80.22663f;                         
             lat2 = 13.05420f;      
             long2 = 80.22676f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
             
             case 34:

             lat1 = 13.05460f;      
             long1 = 80.22657f;                         
             lat2 = 13.05470f;      
             long2 = 80.22769f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 43:
             lat1 = 13.05470f;      
             long1 = 80.22769f;                         
             lat2 = 13.05460f;      
             long2 = 80.22657f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 35:

             lat1 = 13.05460f;      
             long1 = 80.22657f;                         
             lat2 = 13.05553f;      
             long2 = 80.22640f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 53:
             lat1 = 13.05553f;      
             long1 = 80.22640f;                          
             lat2 = 13.05460f;      
             long2 = 80.22657f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 36:

             lat1 = 13.05460f;      
             long1 = 80.22657f;                         
             lat2 = 13.05584f;      
             long2 = 80.22663f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 63:
             lat1 = 13.05584f;      
             long1 = 80.22663f;                       
             lat2 = 13.05460f;      
             long2 = 80.22657f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

            case 45:

             lat1 = 13.05470f;      
             long1 = 80.22769f;                         
             lat2 = 13.05553f;      
             long2 = 80.22640f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 54:
             lat1 = 13.05553f;      
             long1 = 80.22640f;                         
             lat2 = 13.05470f;      
             long2 = 80.22769f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 46:
             lat1 = 13.05470f;      
             long1 = 80.22769f;                          
             lat2 = 13.05584f;      
             long2 = 80.22663f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 64:
             lat1 = 13.05584f;      
             long1 = 80.22663f;                         
             lat2 = 13.05470f;      
             long2 = 80.22769f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;
             
             case 56:

             lat1 = 13.05553f;      
             long1 = 80.22640f;                         
              lat2 = 13.05584f;      
             long2 = 80.22663f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

             case 65:
             lat1 = 13.05584f;      
             long1 = 80.22663f;                         
             lat2 = 13.05553f;      
             long2 = 80.22640f;
             print("GPS is Linked");
             stxt.text = "Gps: " + y;
             break;

            

             default:
             break;
         }
    }


  
    public IEnumerator GenerateRoutePrefabs()
    {
        driftText.text = "Please enable GPS";
        yield return new WaitUntil(() => Input.location.isEnabledByUser);
        Input.location.Start();
        

        while (true)
        {

            driftText.text = "LAT : " + Input.location.lastData.latitude + ", LONG : " + Input.location.lastData.longitude;

            if ((Mathf.Abs(Input.location.lastData.latitude - initialCoordinates.gpsLat) < gpsTolerance) && (Mathf.Abs(Input.location.lastData.longitude - initialCoordinates.gpsLong) < gpsTolerance))
            {
                informationText.text = "INITIAL POSITION";
            }

            else if ((Mathf.Abs(Input.location.lastData.latitude - finalCoordinates.gpsLat) < gpsTolerance) && (Mathf.Abs(Input.location.lastData.longitude - finalCoordinates.gpsLong) < gpsTolerance))
            {
                informationText.text = "FINAL DESINATION";
            }

            else
                informationText.text = "FIND A NODAL POINT.";

            if (isAtOriginPoint)
            {
                Vector3 currentPos = Camera.main.transform.position;    //////////value (2)////////////////////////////////////////////////////////////////////////////////////////////////////////// value---------(2)
                

                RouteManager();//////////////////////////////////////////////////////////////////////
            
               
                isAtOriginPoint = false;

            }



            if (instantiatedObjects.Count != 0)// +(instantiatedObjects.Count - 1) - 
            {
                float distance = Vector3.Distance(instantiatedObjects[instantiatedObjects.Count - 1].transform.position, Camera.main.transform.position);
                distanceText.text = "Distance covered : " + distance + " Meters";

               // int distanceA =  Convert.ToInt32(distanceValue).value;

            }

            else
            {
                distanceText.text = "Journey not started";
            }

            
            

         if (instantiatedObjects.Count != 0)//(instantiatedObjects.Count - 1) +
           {
              distance1 =  Vector3.Distance(instantiatedObjects[instantiatedObjects.Count - 1].transform.position, Camera.main.transform.position);

                StartCoroutine(MeterStone());
             
            }

        
            yield return new WaitForEndOfFrame();
        }
    }

    public IEnumerator MeterStone()
    {
        yield return new WaitUntil(() => Input.location.isEnabledByUser);


        /* if(distance1 >= 2 && distance1 < 3) 
                 {
                    label = "1";
                    //audioSource.clip = Resources.Load<AudioClip>("AudioClips/1");
                    audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                    audioSource.Play();
                    yield return new WaitForSeconds(audioSource.clip.length);
                    audioSource.clip = a1;
                    label = "AHEAD";
                    audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                    audioSource.Play ();
                    messageBehavior.ShowMessage(label);

                 }*/

                   if( 249 <= distance1 && distance1 < 250) 
                    {
                        label = "WALK 250 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                   if( 199 <= distance1 && distance1 < 200) 
                    {
                        label = "WALK 200 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                     if( 189 <= distance1 && distance1 < 190) 
                    {
                        label = "WALK 190 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if( 149 <= distance1 && distance1 < 150) 
                    {
                        label = "WALK 150 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                     if( 99 <= distance1 && distance1 < 100) 
                    {
                        label = "WALK 100 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if( 99 <= distance1 && distance1 < 100) 
                    {
                        label = "WALK 100 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                      if( 89 <= distance1 && distance1 < 90) 
                    {
                        label = "WALK 90 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                     if( 49 <= distance1 && distance1 < 50) 
                    {
                        label = "WALK 50 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }
                    
                    if( 19 <= distance1 && distance1 < 20) 
                    {
                        label = "WALK 20 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if( 9 <= distance1 && distance1 < 10) 
                    {
                        label = "WALK 10 m";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

                    if(0 <= distance1 && distance1 < 1) 
                    {
                        label = "REACHED DESTINATION";
                        audioSource.clip = Resources.Load ("AudioClips/" + label) as AudioClip;
                        audioSource.Play();
                        messageBehavior.ShowMessage(label);

                    }

 
    }
  
   

    #endregion

    
    void Update()
    {
       
    }

    #region STRUCTS,ENUMS AND CLASSES

   public struct Coordinates
    {
        public float gpsLat { get; set; }
        public float gpsLong { get; set; }
    }

   public enum DIRECTION
    {
        FORWARD=0,RIGHT=1,LEFT=2,RIGHT45=3,LEFT45=4,BACKWARD=5  
    }

#endregion

#region DIRECTION_VALUES

/*              for (int goStaight = 0; goStaight < 25; goStaight++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 2; goRight++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goLeft = 0; goLeft < 17; goLeft++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 17; goBack++)
                {
                    GameObject go = Instantiate(forwardPrefab, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                

                */








public void Direction12()// main gate to first year block
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 19; goStaight++)//k1//19
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 16; goLeft++)//k2//16
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 1; goLeft++)//k2
                {
                     
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
            /*
                for (int goBack = 0; goBack < 4; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }*/

     }




     public void Direction21() //firstyear block to main gate
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
               
                
                 for (int goStaight = 0; goStaight < 17; goStaight++)//k2
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goRight = 0; goRight < 18; goRight++)//k1
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward; 
                }

                for (int goRight = 0; goRight < 1; goRight++)//k1
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward; 
                }

                

     }


      public void Direction13()//main gate to hostel
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 18; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

               for (int goStaight = 0; goStaight < 53; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 40; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

               
     }

      public void Direction31()//hostel to main gate
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
           
                
                for (int goStaight = 0; goStaight < 40; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                
                for (int goRight = 0; goRight < 53; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 18; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }



     }

      public void Direction14()// main gate to canteen
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 19; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 53; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 45; goRight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 44; goRight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
     }

      public void Direction41()// canteen to main gate
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 53; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 18; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
     }

      public void Direction15()// main gate to civil block
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 19; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 53; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 43; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction51()// civil lock to main gate
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 44; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 53; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goRight = 0; goRight < 18; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction16()// main gate to main block
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 19; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight <53; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
                for (int goLeft = 0; goLeft < 31; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 34, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 34, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction61()// main block to main gate
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

         for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         for (int goLeft = 0; goLeft < 31; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 34, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 53; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 18; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
                

     }

     public void Direction23()// 1st yr block to hostel
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 18; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 50; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 40; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }




     public void Direction32() //hostel to 1st year block
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

                for (int goStaight = 0; goStaight < 43; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 51; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 17; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }


      public void Direction24()//first year block to cafet;;
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 18; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 55; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 44; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }


     }

      public void Direction42()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 44; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 44; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 53; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 17; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

         

     }

      public void Direction25()// first year to civil block;;;
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 18; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
                for (int goLeft = 0; goLeft < 53; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 41; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction52()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 44; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 53; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goBack = 0; goBack < 17; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction26()//1st yeAR TO mainblk
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 17 ;goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

         
                for (int goLeft = 0; goLeft < 53; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 31; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 226, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 226, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction62()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

                 for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 31; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 34, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 53; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 17; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction34()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 43; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 3; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }


                for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 44; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }


     }

      public void Direction43()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 3; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 42; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

               
     }

     public void Direction35()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 40; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

         
                for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 43; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }




     public void Direction53()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 44; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 42; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 180, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
     }


      public void Direction36()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 40; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

         
                for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 47; goLeft++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 31; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 226, go.transform.rotation.eulerAngles.z)));//46'
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 226, go.transform.rotation.eulerAngles.z)));//46'
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction63()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 31; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 34, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 42; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
                
     }

      public void Direction45()//cafet to civil block
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
                for (int goStaight = 0; goStaight < 43; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction54()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 44; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
               for (int goStaight = 0; goStaight < 45; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 44; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction46()//cafet to main blk
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 44; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goStaight = 0; goStaight < 44; goStaight++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 47; goRight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
         
                for (int goRight = 0; goRight < 31; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 46, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 46, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction64()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
             

                for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 31; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 34, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goStaight = 0; goStaight < 47; goStaight++)//k6
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 45; goLeft++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goLeft = 0; goLeft < 44; goLeft++)//k4
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goLeft = 0; goLeft < 1; goLeft++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction56()//civil to main blk
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;

             for (int goStaight = 0; goStaight < 44; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

            
                for (int goBack = 0; goBack < 31; goBack++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 240, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goBack = 0; goBack < 1; goBack++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 240, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

      public void Direction65()
    {
         print("Direction1 is Linked");
         Dirtxt.text="Route: " + dir;
         
                 for (int goStaight = 0; goStaight < 1; goStaight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }
                for (int goLeft = 0; goLeft < 31; goLeft++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y - 34, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                for (int goRight = 0; goRight < 43; goRight++)
                {
                    GameObject go = Instantiate(ChooseArrows, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

                 for (int goRight = 0; goRight < 1; goRight++)
                {
                    GameObject go = Instantiate(diamond, currentPos, Camera.main.transform.rotation);
                    Camera.main.transform.DetachChildren();
                    go.transform.SetPositionAndRotation(currentPos, Quaternion.Euler(new Vector3(0, go.transform.rotation.eulerAngles.y + 90, go.transform.rotation.eulerAngles.z)));
                    instantiatedObjects.Add(go);
                    currentPos += go.transform.forward;
                }

     }

#endregion


}



