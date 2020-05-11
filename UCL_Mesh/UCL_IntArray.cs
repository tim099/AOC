using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UCL.MeshLib {
    public class UCL_IntArray : IDisposable{
        public IntPtr m_Ptr = IntPtr.Zero;
        int m_Len;
        public UCL_IntArray(int _Len) {
            m_Len = _Len;
            Debug.LogWarning("sizeof(int):" + sizeof(int));
            m_Ptr = Marshal.AllocHGlobal(_Len * sizeof(int));
            unsafe {
                int[] numericArr = new int[3] { 2, 4, 6 };
                fixed (int* ptrArr = &numericArr[0]) {
                    //int[] arr = new int[2](ptrArr);
                    int* foundItem = GetElementInArray(ptrArr, 0);//FindInArray(ptrArr, numericArr.Length, 4);
                    if(foundItem != null) {
                        Debug.LogWarning("find!!:" + *foundItem);
                    } else {
                        Debug.LogWarning("Not Found");
                    }
                }
            }
        }
        public unsafe int* FindInArray(int* theArray, int arrayLength, int valueToFind) {
            for(int counter = 0; counter < arrayLength; counter++) {
                if(theArray[counter] == valueToFind) {
                    return (&theArray[counter]);
                }
            }

            return (null);
        }
        public unsafe int* GetElementInArray(int* theArray, int at) {
            return (&theArray[at]);
        }
        public void Dispose() {
            if(m_Ptr == IntPtr.Zero) {
                return;
            }
            Marshal.FreeHGlobal(m_Ptr);
            Debug.LogWarning("UCL_IntArray Dispose()");
            m_Ptr = IntPtr.Zero;
        }
        ~UCL_IntArray() {
            Dispose();
        }
        
    }
}

