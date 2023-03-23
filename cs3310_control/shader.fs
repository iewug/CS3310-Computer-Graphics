#version 330 core

in vec2 screenCoord;
out vec4 FragColor;
uniform vec2 screenSize;
uniform sampler2D envMap;
uniform float ffov;
uniform vec3 cameraLoc;
uniform vec3 lookAt;

#define PI 3.1415926535
// 材质类型
#define MAT_LAMBERTIAN 0
#define MAT_METALLIC 1
#define MAT_DIELECTRIC 2


/**
 * 随机数, rand() returns a value between [0,1]
 */
uint m_u = uint(521288629);
uint m_v = uint(362436069);
uint GetUintCore(inout uint u, inout uint v) //在函数中,修改inout修饰的形参会影响到实参本身
{
	v = uint(36969) * (v & uint(65535)) + (v >> 16);
	u = uint(18000) * (u & uint(65535)) + (u >> 16);
	return (v << 16) + u;
}
float GetUniformCore(inout uint u, inout uint v){
	uint z = GetUintCore(u, v);
	return float(z) / uint(4294967295);
}
float GetUniform(){
	return GetUniformCore(m_u, m_v);
}
uint GetUint(){
	return GetUintCore(m_u, m_v);
}
float rand(){
	return GetUniform();
}
vec2 rand2(){
	return vec2(rand(), rand());
}
vec3 rand3(){
	return vec3(rand(), rand(), rand());
}
vec4 rand4(){
	return vec4(rand(), rand(), rand(), rand());
}
vec3 random_on_unit_sphere(){
	//单位球面上的一点
	vec3 p;
	float theta = rand() * 2.0 * PI;
	float phi   = rand() * PI;
	p.y = cos(phi);
	p.x = sin(phi) * cos(theta);
	p.z = sin(phi) * sin(theta);
	return p;
}


/**
 * ray
 */
struct Ray
{
    vec3 origin;
    vec3 direction;
};
Ray CreateRay(vec3 o, vec3 d)
{
    Ray ray;
    ray.origin = o;
    ray.direction = d;
    return ray;
}
vec3 GetRayLocation(Ray ray, float t)
{
    return ray.origin + t * ray.direction;
}


/**
 * 球体
 */
struct Sphere
{
    vec3 center;
    float radius;
	int materialPtr;//材质数组的下标
    int materialType;//记录材质的类型(譬如MAT_LAMBERTIAN等3种)
}; 
Sphere CreateSphere(vec3 center, float radius, int type, int ptr)
{
	Sphere sphere;
	sphere.center = center;
	sphere.radius = radius;
	sphere.materialPtr = ptr;
	sphere.materialType = type;
	return sphere;
}


/**
 * camera
 */
struct Camera
{
    vec3 lower_left_corner; //摄像机看到的平面的左下角坐标
    vec3 horizontal; //摄像机看到的平面的宽
    vec3 vertical; //摄像机看到的平面的高
    vec3 origin; //摄像机位置
    vec3 u; //垂直于v&w
    vec3 v; //v与w和上方向up在同一平面，且v垂直于w
    vec3 w; //观察点指向摄像机
    float lens_radius; //光圈半径
}camera;
Camera CameraConstructor(vec3 lookfrom, vec3 lookat, vec3 vup, float vfov, float aspect, float aperture, float focus_dist)
{
    /** Input:
	 * lookfrom摄像机位置；lookat观察点位置；vup上方向；fov可视角度(角度制）；aspect宽高比；aperture光圈直径；focus_dist平面距离摄像机距离
	 */
	Camera camera;
    camera.origin = lookfrom;
    camera.lens_radius = aperture / 2;

    float theta = radians(vfov);
    float half_height = tan(theta / 2);
    float half_width = aspect * half_height;

    camera.w = normalize(lookfrom - lookat);
    camera.u = normalize(cross(vup, camera.w));
    camera.v = cross(camera.w, camera.u);
    camera.lower_left_corner = camera.origin
                              - half_width * focus_dist * camera.u
                              - half_height * focus_dist * camera.v
                              - focus_dist * camera.w;

    camera.horizontal = 2 * half_width * focus_dist * camera.u;
    camera.vertical = 2 * half_height * focus_dist * camera.v;

    return camera;
}
Ray CameraGetRay(Camera camera, vec2 uv)
{
	/**生成从摄像机出发的光线
	 * uv[0,1]之间
	 */
	Ray ray = CreateRay(camera.origin, 
		camera.lower_left_corner + 
		uv.x * camera.horizontal + 
		uv.y * camera.vertical - camera.origin);

	return ray;
}


/**
 * 伽马校正
 */
vec3 GammaCorrection(vec3 c)
{
	return pow(c, vec3(1.0 / 2.2));
}


/**
 * 漫反射材质
 */
struct Lambertian
{
	vec3 albedo; // 即brdf中fr=rho/pi中的rho
};
Lambertian CreateLambertian(vec3 albedo)
{
	Lambertian lambertian;
	lambertian.albedo = albedo;
	return lambertian;
}
/**
 * 金属材质
 */
struct Metallic
{
	vec3 albedo;
	float fuzz; //模糊
};
Metallic CreateMetallic(vec3 albedo, float fuzz)
{
	Metallic metallic;
	metallic.albedo = albedo;
	metallic.fuzz = fuzz;
	return metallic;
}
/**
 * 电解质
 */
struct Dielectric
{
	vec3 albedo;
	float ior; //ni/nr
};
Dielectric CreateDielectric(vec3 albedo, float ior)
{
	Dielectric dielectric;
	dielectric.albedo = albedo;
	dielectric.ior = ior;
	return dielectric;
}

//材质数组，存放不同的设计，在initscene中初始化
Lambertian lambertMaterials[4];
Metallic metallicMaterials[4];
Dielectric dielectricMaterials[4];

//记录击中点信息
struct HitRecord
{
	float t;
	vec3 position;
	vec3 normal; //单位法向量
	int materialPtr;//材质数组的下标
	int materialType;//材质的类型
};

// 对光线的折射和反射
// 漫反射
void LambertianScatter(in Lambertian lambertian, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{ // 修改out修饰的形参会更改实参本身
	attenuation = lambertian.albedo;
	scattered.origin = hitRecord.position;
	scattered.direction = hitRecord.normal + random_on_unit_sphere(); //单位球面上的一点
}
// 反射光
vec3 reflect(in vec3 v, in vec3 n)
{
	// Input： v光源指向入射点，n单位法向量
	// Output： 反射光
	return v - 2 * dot(n, v) * n;
}
// 金属材质
void MetallicScatter(in Metallic metallic, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = metallic.albedo;
	scattered.origin = hitRecord.position;
	scattered.direction = reflect(incident.direction, hitRecord.normal) + metallicMaterials[hitRecord.materialPtr].fuzz * random_on_unit_sphere();
}
// 折射光
bool refract(vec3 v, vec3 n, float ni_over_nt, out vec3 refracted)
{
	// Input： v入射光，n单位法向量，ni_over_nt折射率比，refracted折射光
	// Output： 是否发生折射(可能全发射)
	vec3 uv = normalize(v);
	float dt = dot(uv, n);
	float discriminant = 1.0 - ni_over_nt * ni_over_nt * (1.0 - dt * dt); //1-R垂直^2
	if (discriminant > 0.0)
	{
		refracted = ni_over_nt * (uv - n * dt) - n * sqrt(discriminant);
		return true;
	}
	return false;
}
float schlick(float cosine, float ior)
{
	// Schlick Approximation，发生折射的概率会随着入射角而改变
	// Input： cosine angle from normal; ior ni/nr
	float r0 = (1 - ior) / (1 + ior);
	r0 = r0 * r0;
	return r0 + (1 - r0) * pow((1 - cosine), 5);
}
void DielectricScatter(in Dielectric dielectric, in Ray incident, in HitRecord hitRecord, out Ray scattered, out vec3 attenuation)
{
	attenuation = dielectric.albedo;
	vec3 reflected = reflect(incident.direction, hitRecord.normal);
	vec3 outward_normal;
	float ni_over_nt;
	float cosine;

	if(dot(incident.direction, hitRecord.normal) > 0.0){//从内击中
		outward_normal = -hitRecord.normal;
		ni_over_nt = dielectric.ior;
		cosine = dot(incident.direction, hitRecord.normal) / length(incident.direction);//入射光线角度
	}
	else{//从外击中
		outward_normal = hitRecord.normal;
		ni_over_nt = 1.0 / dielectric.ior;
		cosine = -dot(incident.direction, hitRecord.normal) / length(incident.direction);//入射光线角度
	}

	float reflect_prob;
	vec3 refracted;
	if(refract(incident.direction, outward_normal, ni_over_nt, refracted)){ //能折射
		reflect_prob = schlick(cosine, dielectric.ior);
	}
	else{ //全反射
		reflect_prob = 1.0;
	}

	if(rand() < reflect_prob){ //反射
		scattered = Ray(hitRecord.position, reflected);
	}
	else{ //折射
		scattered = Ray(hitRecord.position, refracted);
	}
}


/**
 * World 记录世界中的物体
 */
struct World
{
    int objectCount; //物体数
    Sphere objects[20]; //最多20个物体
};
World CreateWorld()
{
	World world;
	world.objectCount = 7;
	world.objects[0] = CreateSphere(vec3(0, 1, 0), 1.0, MAT_DIELECTRIC, 0);
	world.objects[1] = CreateSphere(vec3(-4, 1, 0), 1.0, MAT_LAMBERTIAN, 1);
	world.objects[2] = CreateSphere(vec3(4, 1, 0), 1.0, MAT_METALLIC, 0);
	world.objects[3] = CreateSphere(vec3(5, 0.2, -2), 0.2, MAT_DIELECTRIC, 4 % 4);
	world.objects[4] = CreateSphere(vec3(6, 0.2, 0), 0.2, MAT_METALLIC, 5 % 4);
	world.objects[5] = CreateSphere(vec3(4, 0.2, 3), 0.2, MAT_LAMBERTIAN, 6 % 4);
	world.objects[6] = CreateSphere(vec3(-1, 0.2, 4), 0.2, MAT_LAMBERTIAN, 7 % 4);
	return world;
}
void InitScene()
{ 
	// create world
	World world = CreateWorld();

	// create camera
	float aspect_ratio = screenSize.x / screenSize.y;
	//vec3 lookfrom = vec3(20, 1, 40);
    //vec3 lookat = vec3(0, 0, 0);
    vec3 vup = vec3(0, 1, 0);
    float dist_to_focus = 10.0;
    float aperture = 0.1;

    camera = CameraConstructor(cameraLoc, lookAt, vup, ffov, aspect_ratio, aperture, dist_to_focus);

	// init material array. each material has four different types
    lambertMaterials[0] = CreateLambertian(vec3(0.5, 0.5, 0.5));
	lambertMaterials[1] = CreateLambertian(vec3(0.7, 0.5, 0.2));
	lambertMaterials[2] = CreateLambertian(vec3(0.8, 0.3, 0.4));
	lambertMaterials[3] = CreateLambertian(vec3(0.0, 0.7, 0.7));

	metallicMaterials[0] = CreateMetallic(vec3(0.7, 0.6, 0.5), 0.0);
	metallicMaterials[1] = CreateMetallic(vec3(0.5, 0.7, 0.5), 0.1);
	metallicMaterials[2] = CreateMetallic(vec3(0.5, 0.5, 0.7), 0.2);
	metallicMaterials[3] = CreateMetallic(vec3(0.7, 0.7, 0.7), 0.3);

	dielectricMaterials[0] = CreateDielectric(vec3(1.0, 1.0, 1.0), 1.5);
	dielectricMaterials[1] = CreateDielectric(vec3(1.0, 1.0, 1.0), 1.5);
	dielectricMaterials[2] = CreateDielectric(vec3(1.0, 1.0, 1.0), 1.5);
	dielectricMaterials[3] = CreateDielectric(vec3(1.0, 1.0, 1.0), 1.5);
}


/**
 * 光线是否与球体相交
 */
bool HitSphere(Sphere sphere, Ray ray, float t_min, float t_max, inout HitRecord hitRecord)
{
	// 光线ray=o+td, t是否在[t_min,t_max]的范围内与球体sphere有交点。交点信息记录在hitRecord中。
	// 返回是否有交点
	vec3 oc = ray.origin - sphere.center;
	
	float a = dot(ray.direction, ray.direction);
	float b = 2.0 * dot(oc, ray.direction);
	float c = dot(oc, oc) - sphere.radius * sphere.radius;

	float delta = b * b - 4 * a * c;

	if(delta > 0.0) //有交点
	{
		float temp = (-b - sqrt(delta)) / (2.0 * a); //第一个根
		if(temp < t_max && temp> t_min)
		{
			hitRecord.t = temp;
			hitRecord.position = GetRayLocation(ray, hitRecord.t);
			hitRecord.normal = (hitRecord.position - sphere.center) / sphere.radius;

			hitRecord.materialPtr = sphere.materialPtr;
			hitRecord.materialType = sphere.materialType;
			
			return true;
		}

		temp = (-b + sqrt(delta)) / (2.0 * a); //第二个根
		if(temp < t_max && temp> t_min)
		{
			hitRecord.t = temp;
			hitRecord.position = GetRayLocation(ray, hitRecord.t);
			hitRecord.normal = (hitRecord.position - sphere.center) / sphere.radius;

			hitRecord.materialPtr = sphere.materialPtr;
			hitRecord.materialType = sphere.materialType;

			return true;
		}
	}
	return false;
}

// 光线与世界相交
bool HitWorld(World world, Ray ray, float t_min, float t_max, inout HitRecord rec)
{
	// 光线ray=o+td, t是否在[t_min,t_max]的范围内与世界world中的物体有交点。距离最近的交点信息记录在hitRecord中
	// 返回是否有交点
    HitRecord tempRec;
	bool hitanything = false;
	float closest = t_max;//记录最近的击中点的位置

	//最终会获得这条光线击中的最近物体的信息
	for(int i = 0; i < world.objectCount; i++)
	{
	    //如果击中物体，则记录击中点的信息，当击中点比之前的击中点更近时更新信息，更远则不做记录
	    if(HitSphere(world.objects[i], ray, t_min, closest, tempRec))
		{
		    rec = tempRec;
			hitanything = true;
			closest = rec.t;//更新最近的击中点
		}
	}

	return hitanything;
}

// 获取环境贴图颜色
vec3 GetEnvironemntColor(Ray ray)
{
	vec3 dir = normalize(ray.direction);
	float phi = acos(dir.y) / PI;
	float theta = (atan(dir.x, dir.z) + (PI / 2.0)) / PI;
	return texture(envMap, vec2(theta, phi)).rgb;
}


/**
 * 光线追踪！
 */
vec3 RayTrace(Ray ray, int depth)
{
	// Input： 光线ray和递归次数depth
    World world = CreateWorld();

    HitRecord hitRecord;

	vec3 bgColor = vec3(0);//背景颜色
	vec3 objColor = vec3(1.0);//用于累乘，记录光线击中的所有物体的叠加颜色

	while(depth > 0)
	{
	    depth--;
		
		//判断是否击中世界中的物体，如果击中，获得最近的那个物体的信息
		if(HitWorld(world, ray, 0.001, 100000.0, hitRecord))
		{
		    vec3 attenuation;//衰减值，也是物体的颜色
			Ray scatterRay;//散射光线

			//计算出散射光线方向以及获得该物体的颜色
			if(hitRecord.materialType == MAT_LAMBERTIAN){
			    LambertianScatter(lambertMaterials[hitRecord.materialPtr], ray, hitRecord, scatterRay, attenuation);
			}
			else if(hitRecord.materialType == MAT_METALLIC){
			    MetallicScatter(metallicMaterials[hitRecord.materialPtr], ray, hitRecord, scatterRay, attenuation);
			}
			else if(hitRecord.materialType == MAT_DIELECTRIC){
			    DielectricScatter(dielectricMaterials[hitRecord.materialPtr], ray, hitRecord, scatterRay, attenuation);
			}

			ray = scatterRay;//将散射光线作为新的光线
			objColor *= attenuation;//光源衰减，相当于是各个物体颜色的累乘
		}
		//如果没有击中任何物体则退出循环，并获得此时的环境光颜色
		else
		{
		    bgColor = GetEnvironemntColor(ray);
			break;
		}
	}

	//背景颜色相当于是光线，返回物体颜色与光源颜色的乘积
    return objColor * bgColor;
}

void main()
{
    InitScene();
    vec3 color = vec3(0.0, 0.0, 0.0);
	int sampleCount = 100; //每个像素采样100个光线
	for(int i = 0; i < sampleCount; i++)
	{
		Ray ray = CameraGetRay(camera, screenCoord + rand2() / screenSize);
		color += RayTrace(ray, 50);
	}
	color /= sampleCount;
	color = GammaCorrection(color);
	FragColor = vec4(color, 1.0);
}