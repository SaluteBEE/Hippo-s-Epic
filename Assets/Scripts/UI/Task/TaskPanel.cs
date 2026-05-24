using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class TaskPanel : UIWindow
{
    [Header("任务节点")]
    [SerializeField] private GameObject taskOne;
    [SerializeField] private GameObject taskTwo;
    [SerializeField] private GameObject taskThere;
    [SerializeField] private GameObject taskFour;
    
    [Header("地图节点")]
    [SerializeField] private Button btnMidMap1;
    [SerializeField] private Button btnMidMap2;
    [SerializeField] private Button btnMidMap3;
   
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnLast;
    [SerializeField] private Button btnNext;
    
}
