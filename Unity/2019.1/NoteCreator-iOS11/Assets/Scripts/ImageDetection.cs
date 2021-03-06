﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

public class ImageDetection : MonoBehaviour
{
    public GameObject nameplatePrefab;
    public GameObject hiroPrefab;

    CreateNotes createNotesScript;

    private ARTrackedImageManager trackedImageManager;

    private CaliperEventCreator caliperEventCreatorScript;

    // Start is called before the first frame update
    void Start()
    {
        caliperEventCreatorScript = GetComponent<CaliperEventCreator>();

        createNotesScript = GameObject.Find("Interaction").GetComponent<CreateNotes>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            switch (trackedImage.referenceImage.name)
            {
                case "nameplate amanda":
                    GameObject nameplateObj = Instantiate(nameplatePrefab, trackedImage.transform.position, trackedImage.transform.rotation) as GameObject;
                    nameplateObj.transform.parent = trackedImage.transform;
                    nameplateObj.name = "nameplate";

                    createNotesScript.SetAnchorObject(nameplateObj);

                    break;


                case "hiro":
                    GameObject hiroObj = Instantiate(hiroPrefab, trackedImage.transform.position, trackedImage.transform.rotation) as GameObject;
                    hiroObj.transform.parent = trackedImage.transform;
                    hiroObj.name = "hiro";

                    break;
            }

            caliperEventCreatorScript.ImageIdentified(trackedImage.referenceImage.name, trackedImage.referenceImage.guid.ToString());

        }

        foreach (var trackedImage in eventArgs.updated)
        {

        }

        foreach (var trackedImage in eventArgs.removed)
        {

        }
    }
}
