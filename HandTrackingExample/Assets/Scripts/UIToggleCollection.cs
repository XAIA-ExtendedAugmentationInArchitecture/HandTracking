using UnityEngine;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit;
using UnityEngine.Events;
using System.Collections.Generic;

public class UIToggleCollection : ToggleCollection
{
    private List<UnityAction<float>> toggleActions = new List<UnityAction<float>>();

    private List<UIPressableButton> uItoggles = new List<UIPressableButton>();
    
    [SerializeField]
    public List<UIPressableButton> UIToggles
    {
        get => uItoggles;
        set
        {
            if (value != null && uItoggles != value)
            {
                if (uItoggles != null)
                {
                    // Destroy all listeners on previous toggleList
                    RemoveListeners();
                }

                // Set new list
                uItoggles = value;

                // Add listeners to new list
                AddListeners();

            }
        }
    }

    public void InitializeUIToggles()
    {
        // If we don't already have any toggles listed, we scan for toggles
        // in our direct children.
        if (uItoggles == null || uItoggles.Count == 0)
        {
            // Make sure our toggles are not null.
            uItoggles ??= new List<UIPressableButton>();

            // Find some toggles!
            foreach (Transform child in transform)
            {
                var interactable = child.GetComponent<UIPressableButton>();

                // If the interactable is some kind of toggle...
                if (interactable != null && interactable.ToggleMode != StatefulInteractable.ToggleType.Button)
                {
                    uItoggles.Add(interactable);
                }
            }
        }
        if (uItoggles != null && toggleActions.Count == 0)
        {
            // Add listeners to each toggle in ToggleCollection.
            AddListeners();

        }
    }

        
    private void ExecuteAction(UIPressableButton toggle)
    {
        Debug.Log("Action executed for toggle {toggle.gameObject.name}");
        if (toggle == null) { return; }
        if (toggle.Action == UIPressableButton.ActionType.None) { return; }

        // if (toggle.Action == UIPressableButton.ActionType.Drawing_Freehand)

        // Debug.Log("Action executed for toggle {toggle.gameObject.name}");
        // Add custom logic here
    }


    private void AddListeners()
    {
        for (int i = 0; i < Toggles.Count; i++)
        {
            if (Toggles[i] == null) { continue; }
            
            int itemIndex = i;
            UnityAction<float> setSelectionAction = (_) => ExecuteAction(uItoggles[i]);

            toggleActions.Add(setSelectionAction);

            Toggles[i].IsToggled.OnEntered.AddListener(setSelectionAction);
        }
    }

    private void RemoveListeners()
    {
        for (int i = 0; i < toggleActions.Count; i++)
        {
            if (Toggles[i] == null) { continue; }
            
            Toggles[i].IsToggled.OnEntered.RemoveListener(toggleActions[i]);
        }

        toggleActions.Clear();
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }

}
