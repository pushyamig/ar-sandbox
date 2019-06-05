﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// todo save position of note in relation to orientation of sign
// fixme rotation of note always matches rotation of sign even when placed differently

public class CreateNotes : MonoBehaviour
{
    public GameObject placementIndicator;
    public GameObject notePrefab;
    public TMP_InputField inputField;
    public Slider noteSizeSlider;
    public Button placeButton, deleteButton;

    private ARRaycastManager arRaycastManager;
    private Pose placementPose;
    private bool placementPoseIsValid;

    private RaycastHit deleteRayHit;

    public Button saveButton, loadButton;
    public TMP_InputField saveInputField;
    private NoteData noteToSave = null;
    private GameObject anchorObject = null;

    private InfoTextBehavior infoTextBehavior;

    // Start is called before the first frame update
    void Start()
    {
        placeButton.onClick.AddListener(PlaceNote);
        deleteButton.onClick.AddListener(DeleteNote);

        arRaycastManager = FindObjectOfType<ARRaycastManager>();

        //saveButton.onClick.AddListener(() => { saveScript.Save(saveInputField.text, noteToSave); });
        saveButton.onClick.AddListener(SaveNote);
        loadButton.onClick.AddListener(LoadNote);
        saveInputField.text = "save.dat";

        infoTextBehavior = GameObject.Find("Info Text (TMP)").GetComponent<InfoTextBehavior>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();

        UpdatePlaceButton();
        UpdateDeleteButton();
        UpdateSaveButton();

        pinchToZoomNote();
    }

    private void PlaceNote()
    {
        GameObject newNote = Instantiate(notePrefab, placementPose.position, placementPose.rotation) as GameObject;
        newNote.transform.localScale *= noteSizeSlider.value;

        // set text of created note
        NoteBehavior newNoteScript = newNote.GetComponent<NoteBehavior>();
        newNoteScript.changeNoteText(inputField.text);
        inputField.text = "";

        GameObject nameplateObj = GameObject.Find("nameplate");
        if (nameplateObj != null)
        {
            noteToSave = new NoteData(
                newNote.GetComponentInChildren<TMP_Text>().text,
                newNote.transform.position - anchorObject.transform.position,
                newNote.transform.rotation * Quaternion.Inverse(anchorObject.transform.rotation) // order of multiplying quaternions matter!
                );
        }
        else
        {
            infoTextBehavior.SetMessage(2f, Color.red, "nameplate not detected, note will be saved incorrectly!");
        }
    }

    private void DeleteNote()
    {
        Destroy(deleteRayHit.transform.gameObject);
    }

    private void UpdatePlacementPose()
    {
        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinBounds);

        placementPoseIsValid = hits.Count > 0;
        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;

            if (placementPose.rotation.eulerAngles.x < 10) // if looking at horizontal plane
            {
                // rotate note to face camera
                var cameraForward = Camera.main.transform.forward;
                var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;

                placementPose.rotation = Quaternion.LookRotation(cameraBearing);
            }
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
            placementIndicator.transform.localScale = Vector3.one * noteSizeSlider.value;
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    private void UpdatePlaceButton()
    {
        if (placementPoseIsValid && inputField.text != "")
        {
            placeButton.interactable = true;
        }
        else
        {
            placeButton.interactable = false;
        }
    }

    private void UpdateDeleteButton()
    {
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out deleteRayHit, Mathf.Infinity)) // also sets deleteRayHit
        {
            deleteButton.interactable = true;
        }
        else
        {
            deleteButton.interactable = false;
        }
    }

    private void pinchToZoomNote()
    {
        if (Input.touchCount >= 2)
        {
            // find out which two fingers are pinching the same object
            //
            // caveat: only tests touches that are placed in succession.
            //         so it doesn't work if you place one finger on an object,
            //         another somewhere else, and a third on an the same object as finger one
            //         and try to pinch with finger one and three.
            for (int i = 1; i <= Input.touchCount - 1; i++)
            {
                Touch touch0 = Input.GetTouch(i - 1);
                Touch touch1 = Input.GetTouch(i);

                if (touch0.phase == TouchPhase.Moved && touch1.phase == TouchPhase.Moved)
                {
                    Ray touch0Ray = Camera.main.ScreenPointToRay(touch0.position);
                    Ray touch1Ray = Camera.main.ScreenPointToRay(touch1.position);
                    RaycastHit touch0Hit;
                    RaycastHit touch1Hit;
                    bool touch0DidHit = Physics.Raycast(touch0Ray, out touch0Hit);
                    bool touch1DidHit = Physics.Raycast(touch1Ray, out touch1Hit);

                    if (touch0DidHit && touch1DidHit && touch0Hit.transform.gameObject.GetInstanceID() == touch1Hit.transform.gameObject.GetInstanceID())
                    {
                        // once the fingers that are pinching are determined,
                        // find the distance between them this frame
                        // and last frame and scale the object by the dividend.

                        GameObject touchedObject = touch0Hit.transform.gameObject;
                        Debug.Log(touchedObject.GetInstanceID());

                        Vector2 touch0LastPos = touch0.position - touch0.deltaPosition;
                        Vector2 touch1LastPos = touch1.position - touch1.deltaPosition;

                        float touchScale = Vector2.Distance(touch0.position, touch1.position) / Vector2.Distance(touch0LastPos, touch1LastPos);
                        touchedObject.transform.localScale *= touchScale;
                    }
                }
            }
        }
    }

    private void SaveNote()
    {
        var saveScript = GetComponent<PersistentData>();
        saveScript.Save(saveInputField.text, noteToSave);
        infoTextBehavior.SetMessage(2f, Color.green, "Note saved!");
    }

    private void LoadNote()
    {
        var saveScript = GetComponent<PersistentData>();
        NoteData noteData = saveScript.Load(saveInputField.text);

        if (noteData != null)
        {
            GameObject newNote = Instantiate(
                notePrefab,
                noteData.position + anchorObject.transform.position,
                anchorObject.transform.rotation * noteData.rotation // order of multiplying quaternions matter!
                ) as GameObject;
            newNote.transform.localScale *= noteSizeSlider.value;

            NoteBehavior newNoteScript = newNote.GetComponent<NoteBehavior>();
            newNoteScript.changeNoteText(noteData.text);
        }
        else
        {
            infoTextBehavior.SetMessage(2f, Color.red, "No data to load.");
        }
    }

    public void SetAnchorObject(GameObject newAnchorObject)
    {
        anchorObject = newAnchorObject;
    }

    private void UpdateSaveButton()
    {
        if (anchorObject != null && noteToSave != null)
        {
            saveButton.interactable = true;
        }
        else
        {
            saveButton.interactable = false;
        }
    }
}
