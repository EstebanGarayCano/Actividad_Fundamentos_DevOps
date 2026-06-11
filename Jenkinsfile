pipeline {
    agent any

    options {
        skipDefaultCheckout(true)
    }

    environment {
        DOCKERHUB_REPO = 'admicail/ecomify-customers'
        IMAGE_TAG = "${BUILD_NUMBER}"
        DOCKERFILE_PATH = 'EcomifyCustomers/Dockerfile'
        BUILD_CONTEXT = 'EcomifyCustomers'
    }

    stages {
        stage('Clonar repositorio') {
            steps {
                retry(3) {
                    git branch: 'main',
                        url: 'https://github.com/EstebanGarayCano/Actividad_Fundamentos_DevOps.git'
                }
            }
        }

        stage('Verificar archivos del proyecto') {
            steps {
                sh '''
                    echo "Listando archivos del repositorio..."
                    ls -la

                    echo "Verificando Dockerfile..."
                    ls -la EcomifyCustomers
                    test -f EcomifyCustomers/Dockerfile

                    echo "Verificando Docker en Jenkins..."
                    docker --version
                '''
            }
        }

        stage('Construir imagen Docker') {
            steps {
                sh '''
                    echo "Construyendo imagen Docker..."
                    docker build \
                    -t $DOCKERHUB_REPO:$IMAGE_TAG \
                    -t $DOCKERHUB_REPO:latest \
                    -f $DOCKERFILE_PATH \
                    $BUILD_CONTEXT
                '''
            }
        }

        stage('Autenticarse en DockerHub') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'dockerhub-credentials',
                    usernameVariable: 'DOCKERHUB_USER',
                    passwordVariable: 'DOCKERHUB_TOKEN'
                )]) {
                    sh '''
                        echo "Autenticando en DockerHub..."
                        echo "$DOCKERHUB_TOKEN" | docker login -u "$DOCKERHUB_USER" --password-stdin
                    '''
                }
            }
        }

        stage('Publicar imagen en DockerHub') {
            steps {
                sh '''
                    echo "Publicando imagen en DockerHub..."
                    docker push $DOCKERHUB_REPO:$IMAGE_TAG
                    docker push $DOCKERHUB_REPO:latest
                '''
            }
        }
    }

    post {
        success {
            echo 'Pipeline CD ejecutado correctamente. Imagen publicada en DockerHub.'
        }

        failure {
            echo 'El pipeline CD falló. Revisar Console Output.'
        }

        always {
            sh '''
                docker logout || true
            '''
        }
    }
}