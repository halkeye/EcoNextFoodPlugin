pipeline {
  agent { docker { image 'mcr.microsoft.com/dotnet/sdk:5.0' } }

  options {
    buildDiscarder(logRotator(numToKeepStr: '5', artifactNumToKeepStr: '5'))
    timeout(time: 30, unit: 'MINUTES')
    ansiColor('xterm')
  }

  environment {
    HOME="${WORKSPACE}"
  }

  stages {
    stage('Install Dependencies') {
      steps {
        sh "dotnet restore"
      }
    }
    stage('Install Eco Modkit') {
      steps {
        writeFile(file: 'EcoModKit.zip', text: httpRequest(url: 'https://s3-us-west-2.amazonaws.com/eco-releases/EcoModKit_v0.9.1.9-beta.zip').content)
        unzip(zipFile: "EcoModKit_v0.9.1.9-beta.zip", glob: 'ReferenceAssemblies/**/*')
        sh('mv ReferenceAssemblies Dependencies')
      }
    }
    stage('Build') {
      steps {
        dir('NextFood') {
          sh("dotnet publish -c Release -o -no-restore")
        }
      }
    }
    stage('Zip') {
      steps {
        sh("find")
      }
    }
    /*stage('Deploy release') {*/
    /*  when { buildingTag() }*/
    /*  environment { DOCKER = credentials('dockerhub-halkeye') }*/
    /*  steps {*/
    /*    sh 'docker login --username="$DOCKER_USR" --password="$DOCKER_PSW"'*/
    /*    sh "docker tag ${dockerImage} ${dockerImage}:${TAG_NAME}"*/
    /*    sh "docker push ${dockerImage}:${TAG_NAME}"*/
    /*  }*/
    /*}*/
  }
}
