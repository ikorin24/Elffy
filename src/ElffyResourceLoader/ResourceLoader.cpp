#include "ResourceLoader.hpp"

int GetResourceCount() {
    return 10;      // TODO:
}

bool Initialize(int32_t resource_count, int64_t** length, int64_t** position) {
    // TODO:
    for (int i = 0; i < resource_count; i++)
    {
        (*length)[i] = i;
        (*position)[i] = i;
    }
    return true;
}