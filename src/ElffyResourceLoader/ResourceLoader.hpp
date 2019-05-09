#pragma once
#include <cstdint>

extern "C"
{
    __declspec(dllexport) int32_t GetResourceCount();
    __declspec(dllexport) bool Initialize(int32_t, int64_t**, int64_t**);
}
