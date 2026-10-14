# Builds the program that writes testdata/vectors/*.json from the working code of
# Simple Presenter's chord editor (see README.md beside this).
#
#   cmake -S tools/make-vectors -B /tmp/make-vectors -DSIMPLE_PRESENTER=/path/to/Simple-Presenter
#   cmake --build /tmp/make-vectors
#   /tmp/make-vectors/make-vectors testdata/vectors
cmake_minimum_required(VERSION 3.21)
project(make-vectors LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(SIMPLE_PRESENTER "" CACHE PATH "A checkout of github.com/greyshirtguy/Simple-Presenter")
if(NOT EXISTS "${SIMPLE_PRESENTER}/src/chords.cpp")
    message(FATAL_ERROR "Give -DSIMPLE_PRESENTER=<a checkout of Simple-Presenter>")
endif()

find_package(Qt6 REQUIRED COMPONENTS Core Gui)
qt_standard_project_setup()

qt_add_executable(make-vectors
    main.cpp
    fontstub.cpp
    ${SIMPLE_PRESENTER}/src/chords.cpp
    ${SIMPLE_PRESENTER}/src/rtf.cpp
    ${SIMPLE_PRESENTER}/src/richtext.cpp
    ${SIMPLE_PRESENTER}/src/rtfstandins.cpp
)
target_include_directories(make-vectors PRIVATE ${SIMPLE_PRESENTER}/src)
target_link_libraries(make-vectors PRIVATE Qt6::Core Qt6::Gui)
