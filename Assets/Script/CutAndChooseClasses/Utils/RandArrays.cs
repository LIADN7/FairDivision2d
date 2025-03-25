using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RandArrays : MonoBehaviour
{
    private List<float> floatList = new List<float>();

    public static RandArrays inst;

    void Awake()
    {
        // Check if there is another inst of the object
        if (inst != null && inst != this)
        {
            Destroy(gameObject);
            return;
        }

        inst = this;
        DontDestroyOnLoad(gameObject);

        // Check if the loaded scene is "mainS"
        if (SceneManager.GetActiveScene().name == "MainScene")
        {
            Destroy(gameObject);
        }

    }

    // Function to add a number to the List
    public void AddNumber(float number)
    {
        floatList.Add(number);
    }

    // Function to get a number from the List by index
    public float GetNumber(int index)
    {
        // Check if the index is valid
        if (index >= 0 && index < floatList.Count)
        {
            return floatList[index];
        }
        else
        {
            Debug.LogError("Index out of range!");
            return 0f; // Default value
        }
    }

    public void PrintAll()
    {
        this.floatList.ForEach(x => Debug.Log(x));
    }

    // Function to get the size of the List
    public int GetSize()
    {
        return floatList.Count;
    }
}