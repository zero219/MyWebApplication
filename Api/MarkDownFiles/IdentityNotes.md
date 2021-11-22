# Indentity
## 使用
1. 引入包Microsoft.AspNetCore.Identity.EntityFrameworkCore  
2. 增加ApplicationUser类，继承IdentityUser类，这样是为了方便扩展  
3. 用IdentityDbContext\<ApplicationUser>替换DbContext类  
4. OnModelCreating中调用base.OnModelCreating(modelBuilder)
5. 重新关联主键  
6. 添加种子数据；用初始数据登录时可能报错，注意相关的参数是否没有数据
7. Startup类→ConfigureServices中注册identity服务→设置密码、登出等相关设置
8. Startup类→Configure中添加路由、授权、认证等中间件

## UserManager\<ApplicationUser>帮助类
- 用户注册
- 用户登录验证
- 用户数据查询

## SignInManager\<ApplicationUser>帮助类
- 用户登录
- 用户注销

## RoleManager\<ApplicationRole>帮助类
- 角色查询
- 角色添加
- 角色修改

## 基于声明授权(Claim)
- 部分信息，例如：
- 键值对key-value形式
- 来自内部或者外部
- 基于策略(Policy)

## 基于策略的授权(Policy)

### Policy内置方式
- RequireRole
- RequireClaim
- RequireAuthenticatedUser
- RequireUserName
- 还有其它，待研究...

### Policy自定义方式
- AddRequirements
- RequireAssertion