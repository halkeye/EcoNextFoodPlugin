pipeline {
  agent { docker { image 'mcr.microsoft.com/dotnet/sdk:5.0' } }

  options {
    buildDiscarder(logRotator(numToKeepStr: '5', artifactNumToKeepStr: '5'))
    timeout(time: 30, unit: 'MINUTES')
    ansiColor('xterm')
    disableResume()
  }

  environment {
    HOME="${WORKSPACE}"
  }

  stages {
    stage('Install Dependencies') {
      steps {
        sh 'dotnet restore'
      }
    }
    stage('Install Eco Modkit') {
      steps {
        sh('wget -q -O EcoModKit.zip https://s3-us-west-2.amazonaws.com/eco-releases/EcoModKit_v0.9.1.9-beta.zip')
        unzip(zipFile:'EcoModKit.zip', glob: 'ReferenceAssemblies/**/*')
        sh('mkdir -p Dependencies')
        sh('mv ReferenceAssemblies/* Dependencies/')
      }
    }
    stage('Build') {
      steps {
        dir('NextFood') {
          sh('dotnet publish -c Release --no-restore')
        }
      }
    }
    stage('Zip') {
      steps {
        sh('mkdir -p Package/Mods/NextFood Package/Mods/Translations')
        sh('mv NextFood/bin/Release/*/NextFood.dll Package/Mods/NextFood/NextFood.dll')
        sh('mv NextFood/Translations/nextfood.csv Package/Mods/Translations/nextfood.csv')
        sh('cp README.md Package/NextFood.README.md')
        zip(archive: true, dir: 'Package', zipFile: 'NextFood.zip')
      }
    }
    stage('Deploy release') {
      when { branch 'master' }
      environment {
        BEARER_TOKEN = credentials('modio-halkeye')
        GAME_ID = "6"
        MOD_ID = "574190"
      }
      steps {
        sh('''
          export APP_VERSION=$(grep '<Version>' NextFood/EcoNextFoodPlugin.csproj | sed -r 's/.*>([0-9.]+)<.*/\1/g')
          curl -X POST https://api.mod.io/v1/games/${GAME_ID}/mods/${MOD_ID}/files \
            -H "Authorization: Bearer ${BEARER_TOKEN}" \
            -H "Content-Type: multipart/form-data" \
            -H "Accept: application/json" \
            -F "active=true" \
            -F "filedata=@${WORKSPACE}/NextFood.zip" \
            -F "version=${APP_VERSION}_${BUILD_NUMBER}" \
            -F "changelog=${BUILD_URL}changes"
        ''')
      }
    }
  }
  post {
    cleanup {
      cleanWs()
    }
  }
}
