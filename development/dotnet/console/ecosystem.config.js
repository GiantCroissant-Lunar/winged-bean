module.exports = {
  apps: [
    {
      name: 'console-dungeon',
      cwd: '/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/latest/dotnet/bin',
      script: './ConsoleDungeon.Host',
      interpreter: 'none',
      autorestart: true,
      watch: false,
      max_memory_restart: '500M',
      env: {
        DOTNET_ENVIRONMENT: 'Development'
      },
      error_file: './logs/console-dungeon-error.log',
      out_file: './logs/console-dungeon-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true
    }
  ]
};
