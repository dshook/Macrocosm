using UnityEngine;
/// <summary>
/// Attach this script to any gameObject for which you want to put a note.
/// </summary>
public class Note : MonoBehaviour
{
    [TextArea]
    public string Notes = "Comment Here.";
}