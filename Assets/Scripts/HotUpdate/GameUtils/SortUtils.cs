using System;

namespace HotUpdate.GameUtils
{
    public static class SortUtils
    {
        /// <summary>
        /// 冒泡排序
        /// 时间复杂度：平均 O(n²) | 最好 O(n) | 最坏 O(n²) 
        /// 空间复杂度：O(1)
        /// </summary>
        /// <param name="arr">输入int数组</param>
        public static void BubbleSort(int[] arr)
        {
            int n = arr.Length;
            for (int i = 0; i < n - 1; i++)
            {
                bool isSwap = false;
                for (int j = 0; j < n - 1 - i; j++)
                {
                    if (arr[j] > (arr[j + 1]))
                    {
                        (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                        isSwap = true;
                    }
                }

                if (!isSwap)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 选择排序
        /// 时间复杂度：平均 O(n²) | 最好 O(n²) | 最坏 O(n²)
        /// 空间复杂度：O(1)
        /// </summary>
        /// <param name="arr">输入int数组</param>
        public static void SelectSort(int[] arr)
        {
            int n = arr.Length;
            for (int i = 0; i < n - 1; i++)
            {
                int minIndex = i;
                for (int j = i + 1; j < n; j++)
                {
                    if (arr[j] < arr[minIndex])
                    {
                        minIndex = j;
                    }
                }

                if (minIndex != i)
                {
                    (arr[i], arr[minIndex]) = (arr[minIndex], arr[i]);
                }
            }
        }

        /// <summary>
        /// 插入排序(类似扑克牌)
        /// 时间复杂度：平均 O(n²) | 最好 O(n) | 最坏 O(n²)
        /// 空间复杂度：O(1)
        /// </summary>
        /// <param name="arr">输入int数组</param>
        public static void InsertSort(int[] arr)
        {
            int n = arr.Length;
            for (int i = 1; i < n; i++)
            {
                int j = i;
                while (j > 0 && arr[j - 1] > arr[j])
                {
                    (arr[j - 1], arr[j]) = (arr[j], arr[j - 1]);
                    j--;
                }
            }
        }

        /// <summary>
        /// 快速排序
        /// 时间复杂度：平均 O(n*log₂n) | 最好 O(n*log₂n) | 最坏 O(n²)
        /// 空间复杂度：O(log₂n)
        /// </summary>
        /// <param name="arr">输入int数组</param>
        /// <param name="low">起始位置</param>
        /// <param name="high">结束位置</param>
        public static void QuickSort(int[] arr, int low, int high)
        {
            if (low < high)
            {
                int pivotPos = Partition(arr, low, high);
                QuickSort(arr, low, pivotPos - 1);
                QuickSort(arr, pivotPos + 1, high);
            }
        }

        /// <summary>
        /// 快速排序基准算法
        /// </summary>
        /// <param name="arr">输入int数组</param>
        /// <param name="low">起始位置</param>
        /// <param name="high">结束位置</param>
        /// <returns>基准位置</returns>
        static int Partition(int[] arr, int low, int high)
        {
            int pivot = arr[low];
            while (low < high)
            {
                while (low < high && arr[high] > pivot) --high;
                arr[low] = arr[high];
                while (low < high && arr[low] < pivot) ++low;
                arr[high] = arr[low];
            }

            arr[low] = pivot;
            return low;
        }

        /// <summary>
        /// 泛型冒泡排序
        /// 时间复杂度：平均 O(n²) | 最好 O(n) | 最坏 O(n²)
        /// 空间复杂度：O(1)
        /// </summary>
        /// <typeparam name="T">需实现 IComparable 接口的类型如int,float,string数组</typeparam>
        /// <param name="arr">输入数组</param>
        public static void BubbleSort<T>(T[] arr) where T : IComparable<T>
        {
            int n = arr.Length;
            for (int i = 0; i < n - 1; i++)
            {
                bool isSwap = false;
                for (int j = 0; j < n - 1 - i; j++)
                {
                    if (arr[j].CompareTo(arr[j + 1]) > 0)
                    {
                        (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                        isSwap = true;
                    }
                }

                if (!isSwap)
                {
                    break;
                }
            }
        }
    }
}