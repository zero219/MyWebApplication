## Redis在windows中启动  
**redis中redis.windows.conf和redis.windows-service.conf文件的区别**   
Redis由Windows自启动的，配置文件（redis.windows.conf）的设置都是无效的

**解决方案**   
禁用Redis的自启动，设置为手动   
不要使用Redis安装版，使用压缩版   
通过命令行CMD加载配置文件（redis.windows.conf）启动  

**开启多个服务**   
复制redis.windows-service.conf，重命名redis.windows-service-6380.conf 
用记事本打开，并修改端口为6380

**使用cmd进入到redis的安装目录**   
**安装服务**   
redis-server --service-install redis.windows.conf --loglevel verbose  --service-name Redis   
redis-server --service-install redis.windows-service-6380.conf --loglevel verbose  --service-name Redis6380

**启动服务**   
redis-server --service-start --service-name Redis  
redis-server --service-start --service-name Redis6380

**运行**   
redis-cli.exe -h 127.0.0.1 -p 6379   
redis-cli.exe -h 127.0.0.1 -p 6380

**停止服务**   
redis-server --service-stop --service-name Redis   
redis-server --service-stop --service-name Redis6380  

**卸载服务**   
redis-server --service-uninstall --service-name Redis   
redis-server --service-uninstall --service-name Redis6380

## Redis可视化客户端下载
https://github.com/uglide/RedisDesktopManager/releases/tag/0.9.3  
时长更新版 https://github.com/lework/RedisDesktopManager-Windows/releases
