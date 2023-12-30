﻿using System.Runtime.InteropServices;

namespace GGML.Native.Binding.Definitions
{
    public unsafe struct LLamaModelParams
    {
        public int n_gpu_layers;
        public int main_gpu;
        public float* tensor_split;
        public LlamaProgressCallback progress_callback;
        public void* progress_callback_user_data;
        public LlamaModelKvOverride* kv_overrides;
        public bool vocab_only;
        public bool use_mmap;
        public bool use_mlock;
    }
}