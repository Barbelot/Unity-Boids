using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Flock.Sample
{
    [ExecuteInEditMode]
    public class GridManager : MonoBehaviour
    {
        private static readonly int kGridDimensionX = 104;
        private static readonly int kGridDimensionY = 64;
        private static readonly int kCellCapacity = 16;
        private static readonly int kCellCount = kGridDimensionX * kGridDimensionY * kCellCapacity;
        private static readonly int kClearDispatchSize = kGridDimensionX * kGridDimensionY / 8;

        static readonly int kGlobal_Grid_CountID = Shader.PropertyToID("Global_Grid_Count");
        static readonly int kGlobal_Grid_DataID = Shader.PropertyToID("Global_Grid_Data");

        GraphicsBuffer m_NaiveGrid_Count;
        GraphicsBuffer m_NaiveGrid_Data;

        [SerializeField]
        ComputeShader m_GridClear;

        void OnEnable()
        {
            m_NaiveGrid_Count = new GraphicsBuffer(GraphicsBuffer.Target.Structured, kCellCount, Marshal.SizeOf(typeof(uint)));
            m_NaiveGrid_Data = new GraphicsBuffer(GraphicsBuffer.Target.Structured, kCellCount, Marshal.SizeOf(typeof(float)) * 4);

            Shader.SetGlobalBuffer(kGlobal_Grid_CountID, m_NaiveGrid_Count);
            Shader.SetGlobalBuffer(kGlobal_Grid_DataID, m_NaiveGrid_Data);
        }

        void Update()
        {
            //This clear dispatch will be called before any VFX.Update
            m_GridClear?.Dispatch(0, kClearDispatchSize, 1, 1);
        }

        void OnDisable()
        {
            Shader.SetGlobalBuffer(kGlobal_Grid_CountID, (GraphicsBuffer)null);
            Shader.SetGlobalBuffer(kGlobal_Grid_DataID, (GraphicsBuffer)null);

            m_NaiveGrid_Count?.Release();
            m_NaiveGrid_Data?.Release();
        }
    }
}