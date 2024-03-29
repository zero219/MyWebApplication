﻿## HTTP状态码
#### 1.xx
> 信息性状态码webapi中不使用
#### 2.xx
>请求执行成功
> > 200 - OK请求成功  
> > 201 - Created请求成功并创建了资源  
> > 204 - Not Content请求成功但不返回任何东西，例如删除操作  
#### 3.xx
>用于跳转
 #### 4.xx
 >客户端错误
 >>400 - Bad Request,表示API消费者发送服务器的请求错误  
 >>401 - Unauthorized,表示没有提供授权信息或提供授权信息不正确  
 >>403 - Forbidden,表示身份认证已经成功，但是已认证的用户却无法访问请求  
 >>404 - Not Found,表示请求资源不存在  
 >>405 - Method not allowed,当尝试发送请求资源的时候，使用了不被支持的HTTP方法是，就会返回405状态码  
 >>406 - Not acceptable,表示API消费者请求的标书格式并不被Web API所支持，并且API不会提供默认的表述格式。  
 >>409 - Confict,表示请求与服务器当前状态冲突，通常指更新资源时发生的冲突  
 >>415 - Unsupported media type,与406正好相反，有一些请求必须带着数据发往服务器，这些数据都是属于特定的媒体类型，如果API不支持该媒体类型给是，415就会被返回。  
 >>422 - Unprocessable entity,它是HTTP拓展协议的一部分。它说明服务器已经懂得了实体的Content Type,也就是说415状态码肯定不合适；此外，实体的语法也没有问题，所以400也不合适。  
      但是服务器任然无法处理这个实体数据，这是就可以返回422。所以他通常是用来表示语义上有错误，通常就表示实体验证的错误。
#### 5.xx
 >服务器错误
 >>500 - Internal server error,表示服务器出现错误，客户端无能为力，只能以后再试试。  
 **错误Errors：错误是有API的消费者引起的。请求数据不合理就会将其拒绝。**
 
#### HTTP 4xx错误
 不会对API整体可用性造成影响  
 故障 Faults:
 针对一个合理请求，API无法返回他的响应。  
#### HTTP 5xx错误  
会对API整体可用性造成影响

## Restful特点
1. 无状态  
2. 面向资源  
3. 使用http的动词  
4. Hatoas超媒体及应用状态引擎  
5. 对于增删改差好用,面对过程不好用,允许项目中不是Restful风格api

## Restful的6个约束
前后端分离  
无状态→请求独立  
分层系统→代码分层  
统一数据格式→json、xml;自我发现→分页功能  
可缓存  
按需代码  

## Resutful级别成熟度
#### 级别一
有api,通过http传输

#### 级别二
面向资源

#### 级别三
http语法

#### 级别四
超媒体应用状态引擎的实现
api自我发现
超媒体=多媒体+超文本

#### 推荐网站
github、豆瓣

#### 内容协商(统一格式)
传入json、xml格式  
Content-Type application/json、application/xml  
返回json、xml格式  
Aceept application/json、application/xml  
.net core中默认是json格式,xml需要设置  

- DTO是面向界面,面向UI  
- Model面向业务  
- OutoMapper使Dto和Model相互转换

## 
#### HttpHead
 - Head和Get几乎一样,但Head没有请求体(Body)  
 - 可以用来检测缓存  
 - 检测资源是否存在

#### 获取所有资源
`GET` `api/companies`
#### 获取单个资源
`GET` `api/companies/{companyId}`

#### 获取子资源
`GET` `api/companies/{companyId}/employees`
#### 获取单个子资源
`GET` `api/companies/{companyId}/employees/{employeeId}`

#### 数据过滤
`GET` `api/companies?CompanyName=Tencent`

## 封装资源过滤

#### 幂等性和安全性
幂等性：不管多少次调用,返回结果是一致的  
安全性：不会产生副作用,不会改变当前状态;可以被缓存,对资源无损预加载方法。

#### 创建资源
 注：读取和更新Dto分开  
 Hatoas：返回的Header中的location的地址

`POST` `api/company`

添加格式：  
```json
{  
  "companyName":"wangyi",  
  "introduction":"游戏多"  
}  
```

#### 创建子资源
`POST` `api/companies/{companyId}/employee`

添加格式：
```json
{
   "employeeNo":"001",
   "firstName":"li",
   "lastName":"si",
   "gender":1,
   "DateOfBirth":"1996-01-22"
}
```

#### 创建父子资源
`POST` `api/company`
```json
{
	"companyName": "wangyi",
	"introduction": "游戏多",
	"employees": [{
		"employeeNo": "001",
		"firstName": "li",
		"lastName": "si",
		"gender": 1,
		"DateOfBirth": "1996-01-22"
	}, {
		"employeeNo": "002",
		"firstName": "wang",
		"lastName": "wu",
		"gender": 0,
		"DateOfBirth": "1996-01-23"
	}]
}
```

#### 批量添加
`POST` `api/companies`
```json
[{
    "companyName":"wangyi",
    "introduction":"游戏多",
	"employees": [{
		"employeeNo": "001",
		"firstName": "li",
		"lastName": "si",
		"gender": 1,
		"DateOfBirth": "1996-01-22"
	}, {
		"employeeNo": "002",
		"firstName": "wang",
		"lastName": "wu",
		"gender": 0,
		"DateOfBirth": "1996-01-23"
	}]
},{
    "companyName":"huawei",
    "introduction":"狼性文化",
	"employees": [{
		"employeeNo": "001",
		"firstName": "zhao",
		"lastName": "liu",
		"gender": 1,
		"DateOfBirth": "1996-01-24"
	}, {
		"employeeNo": "002",
		"firstName": "sun",
		"lastName": "qi",
		"gender": 0,
		"DateOfBirth": "1996-01-25"
	}]
}]
```

#### 批量查询
GET api/companies/(id1,id2)

## OPTION请求,获取某个WebApi通信选项的信息

#### xml格式
```xml
<CompanyAddDto xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/Entity.Dtos">
 <CompanyName>wangyi</CompanyName>
 <Introduction>游戏多</Introduction>
</CompanyAddDto>
```

#### 实体验证
属性验证  
自定义验证:属性级别、类级别  
推荐使用FluentValidation

#### 自定义实体错误422

#### 处理服务器故障

#### 整体更新\替换
`PUT` `api/companies/{companyId}/employees/{employeeId}`
```json
{
    "employeeNo":"001",
    "firstName":"li",
    "lastName":"si",
    "gender":1,
    "DateOfBirth":"1996-01-22"
}
```

#### 整体新增
PUT

#### 局部更新 
jsonpatch官网:http://jsonpatch.com/ json补丁  
>操作
添加   
```json 
[{ "op": "add", "path": "/dateOfBirth", "value": "2020-01-01" }]
```  
删除、删除一个值  
```json 
[{ "op": "remove", "path": "/gender" },{ "op": "remove", "path": "/dateOfBirth/0" }]
```  
替换  
```json
[{ "op": "replace", "path": "/employeeNo", "value": "MSFT123" }]
```  
复制  
```json
[{ "op": "copy", "from": "/employeeNo", "path": "/lastName" }]
```  
移动  
```json
[{ "op": "move", "from": "/gender", "path": "/lastName" }]
```  
测试  
```json
[{ "op": "test", "path": "/firstName", "value": "Nick" }] 
```

`PATCH` `api/companies/c0ba00d5-198b-49a3-90a0-3dcc764c57c9/employees/4b501cb3-d168-4cc0-b375-48fb33f318a4`

```json
[
    { "op": "add", "path": "/dateOfBirth", "value": "2020-01-01" },
    { "op": "replace", "path": "/employeeNo", "value": "MSFT123" },
    { "op": "remove", "path": "/gender" },
    { "op": "move", "from": "/gender", "path": "/lastName" },
    { "op": "copy", "from": "/firstName", "path": "/lastName" },
    { "op": "test", "path": "/firstName", "value": "Nick" }
]
```

#### 删除资源
`DELETE` `api/companies/{companyId}/employees/{employeeId}`

#### 分页
`GET` `api/companies?pageNumber=1&pageSize=5`

#### 排序
`GET` `api/companies?orderBy=id desc, companyName`

#### 数据塑形
`GET` `api/companies?fields=companyName,id`  
**可以参考OData**

#### 研究包含子资源 
`GET` `api/companies?expand=employee`  
`GET` `api/companies?fields=employee.id`

#### 高级过滤
`api/companies?companyName.contains("id")`

#### HATEOAS,超媒体链接
links是核心,href包含url、rel描述资源和url的关系、method表示url用到http方法  
> 例子  
```json
{
    "id": "c0ba00d5-198b-49a3-90a0-3dcc764c57c9",
    "companyName": "Microsoft",
    "links": [
        {
            "href": "http://localhost:5000/api/companies/c0ba00d5-198b-49a3-90a0-3dcc764c57c9",
            "rel": "self",
            "method": "GET"
        },
        {
            "href": "http://localhost:5000/api/companies/c0ba00d5-198b-49a3-90a0-3dcc764c57c9",
            "rel": "delete_company",
            "method": "DELETE"
        },
        {
            "href": "http://localhost:5000/api/companies/c0ba00d5-198b-49a3-90a0-3dcc764c57c9/employees",
            "rel": "company_for_self_employees",
            "method": "GET"
        },
        {
            "href": "http://localhost:5000/api/companies/c0ba00d5-198b-49a3-90a0-3dcc764c57c9/employees",
            "rel": "company_for_create_employee",
            "method": "POST"
        }
    ]
}
```

## Media Type
#### 格式解析:   
例: `application/vnd.mycompany.hateoas+json`   
vnd: 供应商缩写  
mycompany: 供应商标识，某某公司名称  
hateoas: 媒体类型名称  
json: json格式  

#### 输入Vendor-specific media type(供应商特定媒体类型)
- HTTP请求头部输入`Accept:application/vnd.company.friendly+json`  
  
- ASP.NET Core中全局注册`application/vnd.company.friendly+json`<b>Media Type</b>格式  
```csharp
public void ConfigureServices(IServiceCollection services){
            services.Configure<MvcOptions>(config =>{
                var newtonSoftJsonOutputFormatter = config.OutputFormatters.OfType<NewtonsoftJsonOutputFormatter>()?.FirstOrDefault();
                newtonSoftJsonOutputFormatter?.SupportedMediaTypes.Add("application/vnd.company.friendly+json");
            });
}
```
#### 输出Vendor-specific media type(供应商特定媒体类型)
- HTTP请求头部输入`Content-Type:application/json`

#### 总结
```
Content-Type 和 Accept 是 HTTP 请求和响应头中的两个关键字段，它们用于指定请求中发送的数据类型和响应中返回的数据类型。

Content-Type（请求头）：
作用： 它告诉服务器实际发送的数据是什么类型。
示例： 当你通过 POST 请求向服务器提交表单数据时，可以使用 
Content-Type: application/x-www-form-urlencoded 或 Content-Type: multipart/form-data 来指定数据格式。

常见值：
application/json: 用于指定请求或响应中的数据是 JSON 格式。
application/x-www-form-urlencoded: 用于指定表单数据的传递方式。
multipart/form-data: 用于指定表单数据的传递方式，支持传递文件。

Accept（请求头）：
作用： 它告诉服务器客户端期望接收的响应数据类型。
示例： 如果客户端只能处理 JSON 格式的数据，可以在请求头中添加 Accept: application/json。
常见值：
application/json: 表示客户端期望接收 JSON 格式的数据。
text/html: 表示客户端期望接收 HTML 格式的数据。
application/xml: 表示客户端期望接收 XML 格式的数据。

总结：
Content-Type 用于指定请求中发送的数据类型。
Accept 用于指定客户端期望接收的响应数据类型。
在实际应用中，这两者的配合可以确保客户端和服务器之间正确地处理请求和响应的数据格式，以确保有效的通信。
```

## HTTP缓存
#### .net core自带的响应缓存  
https://docs.microsoft.com/zh-cn/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0  

##### Startup.cs中注册
```c#

 public void ConfigureServices(IServiceCollection services)
 {
     // 注册ResponseCaching缓存
     services.AddResponseCaching();
     services.AddControllers(setup =>
     {
          #region 全局设置响应缓存过期时间
          setup.CacheProfiles.Add("CacheProfileKey", new CacheProfile
          {
              Duration = 120
          });
          #endregion
     });
 }

 public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
 {
    // 添加ResponseCaching中间件
    app.UseResponseCaching();
 }
```

##### 必须在Controller或者Action上使用才生效
```c#
// CacheProfileName注册时的key,而且必须与之对应; Duration:过期时间;
// 局部添加过期时间会覆盖前者
[ResponseCache(CacheProfileName = "CacheProfileKey", Duration = 60)]
```
添加完以后可以看到HTTP返回带有Cache-Control这个属性；并且第二次有Age这个属性，它表示已缓存的时间  
![如图](Images/Snipaste_2023-12-08_20-37-25.png)
<font color="red">注：使用该缓存时，只有响应状态码为200的Get或者Head请求才可能被缓存，如果请求的Headers中携带了`Authorization`、`Set-Cookie`,直接导致缓存失效。这玩意有点拉闸呀，有时间在研究吧。</font>

#### ETAG验证模型
查看请求头中的指定:https://datatracker.ietf.org/doc/html/rfc7234#section-5.2

##### Startup.cs中注册
```c#
 public void ConfigureServices(IServiceCollection services)
 {
     // 全局注册
     services.AddHttpCacheHeaders(expires =>//过期模型
            {
                //过期时间
                expires.MaxAge = 120;
                //私有的
                expires.CacheLocation = CacheLocation.Private;
            },
            validation =>//验证模型
            {
                //响应过期必须重新验证
                validation.MustRevalidate = true;
            });
 }

 public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
 {
    // 添加中间件,与ResponseCache结合使用时必须在前面
    app.UseHttpCacheHeaders();
    app.UseResponseCache();
 }
```
##### Controller或者Action上使用
```c#
 // 如果全局注册了该缓存，又在Controller或者Action上使用，则过期时间会被后者覆盖
 //ETAG缓存过期模型
 [HttpCacheExpiration(CacheLocation = CacheLocation.Public, MaxAge = 60)]
 //ETAG缓存验证模型
 [HttpCacheValidation(MustRevalidate = true)]
```
相比于框架自带的，多了四个属性 
![如图](Images/Snipaste_2023-12-07_22-58-07.png)

#### 悲观并发控制
锁定当前客户端,只有当前客户端可以修改;但是因为REST有无状态约束,所以无法操作

#### 乐观并发控制
当前客户端得到一个token,携带当前token去更新资源;
只要这个token是合理有效,那么当前用户就可以一直更新资源;
这个token就是一个验证器,而且要求是强验证器,例如ETAG.

请求头中使用
`If-Match`、`If-None-Match` ETAG验证器

## Jwt
网址:https://jwt.io/


## .net core
Ok()返回200
CreatedAtRoute()201
NoContent()204 
NotFound()404

## Autofac
官方文档:https://autofac.readthedocs.io/en/latest/

## log4net

## redis

## rabbitmq


