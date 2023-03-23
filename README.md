# Ray Tracing

<img src="dzy.gif" style="zoom:67%;" />

This is our first project of SJTU CS3310 Computer Graphics. Here is a helper file to guide you how to run our program.

## Installation

You need to set up your OpenGL environment before running our program. Different operating systems have different installation methods. This [link](https://zhuanlan.zhihu.com/p/427278169) may be helpful for those who want to run the program under Ubuntu.

## Demo

In `cs3310` or `cs3310_control` folder

```bash
# compile
g++ main.cpp glad.c -lglfw3 -lGL -lX11 -lpthread -lXrandr -lXi -ldl -lm -lXxf86vm -lXinerama -lXcursor -o main
# run
./main
```

In `cs3310` folder, the model is rotating around middle so that the whole 3D picture can be observed. 

In `cs3310_control` folder, we can change the camera position through mouse or keyboard.
