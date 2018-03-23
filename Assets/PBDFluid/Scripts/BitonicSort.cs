using System;
using System.Collections.Generic;
using UnityEngine;

namespace PBDFluid
{

    public class BitonicSort : IDisposable
    {
        //Range of min/max particles for various block sizes.

        //64 * 2 = 128
        //64 * 64 = 4096

        //128 * 4 = 512
        //128 * 128 = 16384

        //256 * 8 = 2048
        //256 * 256 = 65536

        //512 * 16 = 8192
        //512 * 512 = 262144

        //1024 * 32 = 32768
        //1024 * 1024 = 1048576

        //Num threads for the copy and fill kernels.
        private const int THREADS = 128;

        // The number of elements to sort is limited to an even power of 2.
        // the min/max range of particles for these sizes are shown above.
        // If you need to resize these you must also change the same values in the shader.
        // TODO - Have a shader for each range and automatically pick which one to use.
        private const int BITONIC_BLOCK_SIZE = 512;
        private const int TRANSPOSE_BLOCK_SIZE = 16;

        public const int MAX_ELEMENTS = BITONIC_BLOCK_SIZE * BITONIC_BLOCK_SIZE;
        public const int MIN_ELEMENTS = BITONIC_BLOCK_SIZE * TRANSPOSE_BLOCK_SIZE;

        private const int MATRIX_WIDTH = BITONIC_BLOCK_SIZE;
        
        public int NumElements { get; private set; }

        private ComputeBuffer m_buffer1, m_buffer2;

        private ComputeShader m_shader;

        int m_bitonicKernel, m_transposeKernel;

        int m_fillKernel, m_copyKernel;

        public BitonicSort(int count)
        {
            NumElements = FindNumElements(count);
            m_buffer1 = new ComputeBuffer(NumElements, 2 * sizeof(int));
            m_buffer2 = new ComputeBuffer(NumElements, 2 * sizeof(int));

            m_shader = Resources.Load("BitonicSort") as ComputeShader;
            m_bitonicKernel = m_shader.FindKernel("BitonicSort");
            m_transposeKernel = m_shader.FindKernel("MatrixTranspose");
            m_fillKernel = m_shader.FindKernel("Fill");
            m_copyKernel = m_shader.FindKernel("Copy");
        }

        public void Dispose()
        {
            CBUtility.Release(ref m_buffer1);
            CBUtility.Release(ref m_buffer2);
        }

        public void Sort(ComputeBuffer input)
        {

            int count = input.count;
            if (count < MIN_ELEMENTS)
                throw new ArgumentException("count < MIN_ELEMENTS");

            if (count > NumElements)
                throw new ArgumentException("count > NumElements");

            m_shader.SetInt("Width", count);
            m_shader.SetBuffer(m_fillKernel, "Input", input);
            m_shader.SetBuffer(m_fillKernel, "Data", m_buffer1);
            m_shader.Dispatch(m_fillKernel, NumElements / THREADS, 1, 1);

            int MATRIX_HEIGHT = NumElements / BITONIC_BLOCK_SIZE;

            m_shader.SetInt("Width", MATRIX_HEIGHT);
            m_shader.SetInt("Height", MATRIX_WIDTH);
            m_shader.SetBuffer(m_bitonicKernel, "Data", m_buffer1);

            // Sort the data
            // First sort the rows for the levels <= to the block size
            for (int level = 2; level <= BITONIC_BLOCK_SIZE; level = level * 2)
            {
                // Sort the row data
                m_shader.SetInt("Level", level);
                m_shader.SetInt("LevelMask", level);
                m_shader.Dispatch(m_bitonicKernel, NumElements / BITONIC_BLOCK_SIZE, 1, 1);
            }

            // Then sort the rows and columns for the levels > than the block size
            // Transpose. Sort the Columns. Transpose. Sort the Rows.
            for (int level = (BITONIC_BLOCK_SIZE * 2); level <= NumElements; level = level * 2)
            {
                // Transpose the data from buffer 1 into buffer 2
                m_shader.SetInt("Level", level / BITONIC_BLOCK_SIZE);
                m_shader.SetInt("LevelMask", (level & ~NumElements) / BITONIC_BLOCK_SIZE);
                m_shader.SetInt("Width", MATRIX_WIDTH);
                m_shader.SetInt("Height", MATRIX_HEIGHT);
                m_shader.SetBuffer(m_transposeKernel, "Input", m_buffer1);
                m_shader.SetBuffer(m_transposeKernel, "Data", m_buffer2);
                m_shader.Dispatch(m_transposeKernel, MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE, 1);

                // Sort the transposed column data
                m_shader.SetBuffer(m_bitonicKernel, "Data", m_buffer2);
                m_shader.Dispatch(m_bitonicKernel, NumElements / BITONIC_BLOCK_SIZE, 1, 1);

                // Transpose the data from buffer 2 back into buffer 1
                m_shader.SetInt("Level", BITONIC_BLOCK_SIZE);
                m_shader.SetInt("LevelMask", level);
                m_shader.SetInt("Width", MATRIX_HEIGHT);
                m_shader.SetInt("Height", MATRIX_WIDTH);
                m_shader.SetBuffer(m_transposeKernel, "Input", m_buffer2);
                m_shader.SetBuffer(m_transposeKernel, "Data", m_buffer1);
                m_shader.Dispatch(m_transposeKernel, MATRIX_HEIGHT / TRANSPOSE_BLOCK_SIZE, MATRIX_WIDTH / TRANSPOSE_BLOCK_SIZE, 1);

                // Sort the row data
                m_shader.SetBuffer(m_bitonicKernel, "Data", m_buffer1);
                m_shader.Dispatch(m_bitonicKernel, NumElements / BITONIC_BLOCK_SIZE, 1, 1);
            }

            m_shader.SetInt("Width", count);
            m_shader.SetBuffer(m_copyKernel, "Input", m_buffer1);
            m_shader.SetBuffer(m_copyKernel, "Data", input);
            m_shader.Dispatch(m_copyKernel, NumElements / THREADS, 1, 1);
        }

        private int FindNumElements(int count)
        {
            if (count < MIN_ELEMENTS)
                throw new ArgumentException("Data != MIN_ELEMENTS. Need to decrease Bitonic size.");

            if (count > MAX_ELEMENTS)
                throw new ArgumentException("Data > MAX_ELEMENTS. Need to increase Bitonic size");

            int NumElements;

            int level = TRANSPOSE_BLOCK_SIZE;
            do
            {
                NumElements = BITONIC_BLOCK_SIZE * level;
                level *= 2;
            }
            while (NumElements < count);

            return NumElements;
        }

    }

}
