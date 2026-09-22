cmake_minimum_required(VERSION 3.25)
project(wet LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(CMAKE_SYSTEM_PROCESSOR MATCHES "AMD64|x86_64|amd64")
    add_compile_options(-march=x86-64-v2)
endif()

if(NOT REXSDK_DIR AND EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/thirdparty/rexglue-sdk/CMakeLists.txt")
    set(REXSDK_DIR "${CMAKE_CURRENT_SOURCE_DIR}/thirdparty/rexglue-sdk" CACHE PATH
        "Path to rexglue-sdk source tree" FORCE)
endif()

include(generated/rexglue.cmake)

set(WET_SOURCES src/main.cpp)

if(WIN32)
    enable_language(RC)
    add_executable(wet WIN32 ${WET_SOURCES} src/wet.rc)
else()
    add_executable(wet ${WET_SOURCES})
endif()

rexglue_setup_target(wet GPU_PLUGINS xenos)
target_include_directories(wet PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/thirdparty/rexglue-sdk/thirdparty/imgui")
