/**
 * CS3310 ray tracing project 1
 * enable keyboard and mouse control
 * g++ main.cpp glad.c -lglfw3 -lGL -lX11 -lpthread -lXrandr -lXi -ldl -lm -lXxf86vm -lXinerama -lXcursor -o main
 * ./main
*/


// openGL只是一个标准/规范。glfw提供渲染物体所需接口，glad管理opengl函数指针
#include <glad/glad.h>
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>

#include "header/shader.h"
#define STB_IMAGE_IMPLEMENTATION
#include "header/stb_image.h"

// Standard Headers
#include <cstdio>
#include <cstdlib>
#include <iostream>

// 函数声明
void framebuffer_size_callback(GLFWwindow *window, int width, int height);//回调函数原型声明
void mouse_callback(GLFWwindow* window, double xpos, double ypos);
void scroll_callback(GLFWwindow* window, double xoffset, double yoffset);
void processInput(GLFWwindow *window); //输入函数原型
unsigned int loadTex(char const* path); //加载纹理

// settings，屏幕的宽和高
const unsigned int SCR_WIDTH = 1600;
const unsigned int SCR_HEIGHT = 800;
float fov   =  20.0f;
// timing
float deltaTime = 0.0f;	// time between current frame and last frame
float lastFrame = 0.0f;
// camera
glm::vec3 cameraPos   = glm::vec3(10.0f, 1.0f, 3.0f);
glm::vec3 cameraUp    = glm::vec3(0.0f, 1.0f, 0.0f);
glm::vec3 lookAt = glm::vec3(0.0f,0.0f,0.0f);
bool firstMouse = true;
float lastX =  0.0f;
float lastY =  0.0f;


int main()
{
    //初始化GLFW
    glfwInit();
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3); //主版本号3
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3); //次版本号3
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE); //核心模式，不向后兼容
    //创建一个窗口对象
    GLFWwindow *window = glfwCreateWindow(SCR_WIDTH, SCR_HEIGHT, "ray tracing", NULL, NULL); //窗口宽高和标题
    if (window == NULL) {
        std::cout << "Failed to create GLFW window" << std::endl;
        glfwTerminate();
        return -1;
    }
    //通知GLFW将我们窗口的上下文设置为当前线程的主上下文
    glfwMakeContextCurrent(window);
    //对窗口注册一个回调函数,每当窗口改变大小，GLFW会调用这个函数并填充相应的参数供你处理
    //这里是让渲染区域随着窗口大小动态改变
    glfwSetFramebufferSizeCallback(window, framebuffer_size_callback);
    glfwSetCursorPosCallback(window, mouse_callback);
    glfwSetScrollCallback(window, scroll_callback);
    glfwSetInputMode(window, GLFW_CURSOR, GLFW_CURSOR_DISABLED);

    //初始化GLAD用来管理OpenGL的函数指针
    if (!gladLoadGLLoader((GLADloadproc)glfwGetProcAddress)) {
        std::cout << "Failed to initialize GLAD" << std::endl;
        return -1;
    }

    // build and compile our shader program
    // ------------------------------------
    Shader ourShader("shader.vs", "shader.fs");
    unsigned int hdrTexture = loadTex("img/6.jpg");
    
    // set up vertex data (and buffer(s)) and configure vertex attributes
    // ------------------------------------------------------------------
    // 标准化设备坐标NDC 范围在-1和1之间。z坐标设置成一样，看上去2D
    float vertices[] = {
		 1.0f,  1.0f, 0.0f,  // top right
		 1.0f, -1.0f, 0.0f,  // bottom right
		-1.0f, -1.0f, 0.0f,  // bottom left
		-1.0f,  1.0f, 0.0f   // top left 
	};
	unsigned int indices[] = {  // note that we start from 0!
		0, 1, 3,   // first triangle
		1, 2, 3    // second triangle
	};

    unsigned int VBO, VAO, EBO;
	glGenVertexArrays(1, &VAO);
	glBindVertexArray(VAO);

	glGenBuffers(1, &VBO);
	glBindBuffer(GL_ARRAY_BUFFER, VBO);
	glBufferData(GL_ARRAY_BUFFER, sizeof(vertices), vertices, GL_STATIC_DRAW);

	glGenBuffers(1, &EBO);
	glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
	glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(indices), indices, GL_STATIC_DRAW);

	// position attribute
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
    glEnableVertexAttribArray(0);

    int width;
    int height;
    //渲染循环
    while(!glfwWindowShouldClose(window)) //在我们每次循环的开始前检查一次GLFW是否被要求退出
    {
        float currentFrame = static_cast<float>(glfwGetTime());
        deltaTime = currentFrame - lastFrame;
        lastFrame = currentFrame;
        
        // 输入
        processInput(window);

        // // 渲染指令，使用一个颜色来清屏
        // glClearColor(0.2f, 0.3f, 0.3f, 1.0f); //设置清屏颜色
        // glClear(GL_COLOR_BUFFER_BIT); //清屏

        // render container
       
        glBindVertexArray(VAO);//之前绑定过了，这里可以不绑定
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0); // draw two triangles
        
        glfwGetWindowSize(window, &width, &height); //实时窗口大小

        ourShader.use();
        glActiveTexture(GL_TEXTURE0); //激活纹理0
		glBindTexture(hdrTexture, 0); //将纹理图片绑定
        //设置uniform变量
        ourShader.setInt("envMap", 0);
        ourShader.setVec2("screenSize", width, height);
        ourShader.setFloat("ffov",fov);
        ourShader.setVec3("cameraLoc",cameraPos);
        ourShader.setVec3("lookAt", lookAt);

        // 检查并调用事件，交换缓冲
        glfwSwapBuffers(window);//交换颜色缓冲，将渲染在后缓冲的内容交给前缓冲，呈现出来
        glfwPollEvents();//检查有没有触发什么事件（比如键盘输入、鼠标移动等）、更新窗口状态，并调用对应的回调函数
    }
    
    
    // optional: de-allocate all resources once they've outlived their purpose:
    // ------------------------------------------------------------------------
    glDeleteVertexArrays(1, &VAO);
    glDeleteBuffers(1, &VBO);
    glDeleteBuffers(1, &EBO);

    //释放/删除之前的分配的所有资源
    glfwTerminate();
    return 0;
}

//输入控制，检查用户是否按下了返回键(Esc)
void processInput(GLFWwindow *window)
{
    if(glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS) //如果按下esc
    {
        glfwSetWindowShouldClose(window, true); //则要求glfw退出
    }
    float cameraSpeed = static_cast<float>(2.5 * deltaTime);
    glm::vec3 aaa = lookAt - cameraPos;
    if (glfwGetKey(window, GLFW_KEY_W) == GLFW_PRESS)
    {
        cameraPos += cameraSpeed * aaa;
    }     
    if (glfwGetKey(window, GLFW_KEY_S) == GLFW_PRESS)
        cameraPos -= cameraSpeed * aaa;
    if (glfwGetKey(window, GLFW_KEY_A) == GLFW_PRESS)
        cameraPos -= glm::normalize(glm::cross(aaa, cameraUp)) * cameraSpeed;
    if (glfwGetKey(window, GLFW_KEY_D) == GLFW_PRESS)
        cameraPos += glm::normalize(glm::cross(aaa, cameraUp)) * cameraSpeed;
}

// 当用户改变窗口的大小的时候，视口（渲染窗口）也应该被调整
void framebuffer_size_callback(GLFWwindow *window, int width, int height) {
    // 注意：对于视网膜(Retina)显示屏，width和height都会明显比原输入值更高一点。
    glViewport(0, 0, width, height);
}

//加载纹理
unsigned int loadTex(char const* path)
{
	unsigned int textureID;
	glGenTextures(1, &textureID);

	// stbi_set_flip_vertically_on_load(true); //图像加载时翻转y轴

	int width, height, nrComponents;
	unsigned char* data = stbi_load(path, &width, &height, &nrComponents, 0);
	if (data)
    {
		GLenum format;
		GLenum iformat;
		if (nrComponents == 1)
        {
			format = GL_RED;
			iformat = GL_RED;
		}
		else if (nrComponents == 3) //.jpg
        { 
			format = GL_RGB;
			iformat = GL_RGB;
		}
		else //.png
        {
			format = GL_RGBA;
			iformat = GL_RGBA;
		}

		glBindTexture(GL_TEXTURE_2D, textureID); //绑定纹理
		glTexImage2D(GL_TEXTURE_2D, 0, iformat, width, height, 0, format, GL_UNSIGNED_BYTE, data); //生成纹理
		glGenerateMipmap(GL_TEXTURE_2D); //生成mipmap
        // 为当前绑定的纹理对象设置环绕、过滤方式
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	}
	else
    {
		std::cout << "Failed to load texture: " << path << std::endl;
	}
    stbi_image_free(data);//释放图像内存
	return textureID;
}

// 鼠标滚轮修改fov
void scroll_callback(GLFWwindow* window, double xoffset, double yoffset)
{
    fov -= (float)yoffset;
    if (fov < 1.0f)
        fov = 1.0f;
    if (fov > 45.0f)
        fov = 45.0f;
}

void mouse_callback(GLFWwindow* window, double xposIn, double yposIn)
{
    float xpos = static_cast<float>(xposIn);
    float ypos = static_cast<float>(yposIn);

    if (firstMouse)
    {
        lastX = xpos;
        lastY = ypos;
        firstMouse = false;
    }

    float xoffset = xpos - lastX;
    float yoffset = lastY - ypos; // reversed since y-coordinates go from bottom to top
    lastX = xpos;
    lastY = ypos;

    float sensitivity = 0.1f; // change this value to your liking
    xoffset *= sensitivity;
    yoffset *= sensitivity;

    lookAt.x += xoffset;
    lookAt.y += yoffset;

    
}